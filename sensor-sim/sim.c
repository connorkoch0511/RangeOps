/*
 * RangeOps sensor-sim
 * -------------------
 * A minimal instrumentation "rig" that models a climbing test aircraft and
 * streams telemetry to any connected client over TCP as newline-delimited
 * JSON, five samples per second.
 *
 * It also periodically injects a "stuck altimeter" fault so downstream
 * consumers (the operator console) have a fault condition to detect and log --
 * the same kind of fault-injection used in HIL/SIL flight-test rigs.
 *
 * Protocol (one JSON object per line):
 *   {"alt_ft":12345.6,"airspeed_kt":320.4,"vs_fpm":1800.0,"fault":false}
 *
 * Usage:  ./rangeops-sim [port]      (default port 5555, or $SIM_PORT)
 */

#include <arpa/inet.h>
#include <netinet/in.h>
#include <signal.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/socket.h>
#include <time.h>
#include <unistd.h>

#define DEFAULT_PORT 5555
#define SAMPLE_HZ 5
#define CRUISE_ALT_FT 25000.0
#define CRUISE_KT 320.0

static volatile sig_atomic_t g_running = 1;

static void on_sigint(int sig) {
    (void)sig;
    g_running = 0;
}

/* small +/- noise in [-mag, mag] */
static double noise(double mag) {
    return ((double)rand() / RAND_MAX) * 2.0 * mag - mag;
}

static int resolve_port(int argc, char **argv) {
    if (argc > 1) return atoi(argv[1]);
    const char *env = getenv("SIM_PORT");
    if (env && *env) return atoi(env);
    return DEFAULT_PORT;
}

/* Stream one flight profile to a connected client until it disconnects
 * or we're asked to shut down. Returns 0 on clean client disconnect. */
static int stream_flight(int client_fd) {
    double alt = 0.0;          /* true altitude, ft            */
    double airspeed = 140.0;   /* kt                            */
    double reported_alt = 0.0; /* what the altimeter reports    */
    long tick = 0;
    const long fault_start = 8 * SAMPLE_HZ;  /* inject at t=8s   */
    const long fault_end = 14 * SAMPLE_HZ;   /* clear at t=14s   */
    char line[128];
    struct timespec dt = {0, (1000000000L / SAMPLE_HZ)};

    while (g_running) {
        /* --- flight model: climb toward cruise, then hold --- */
        double vs; /* vertical speed, fpm */
        if (alt < CRUISE_ALT_FT) {
            vs = 2000.0 + noise(120.0);
            alt += vs / 60.0 / SAMPLE_HZ; /* fpm -> ft per sample */
            if (airspeed < CRUISE_KT) airspeed += 0.4;
        } else {
            alt = CRUISE_ALT_FT;
            vs = noise(80.0);
        }
        airspeed += noise(1.5);

        /* --- fault injection: altimeter freezes for a window --- */
        int fault = (tick >= fault_start && tick < fault_end);
        if (!fault) {
            reported_alt = alt + noise(15.0); /* healthy: track truth + noise */
        } /* else: hold last reported_alt -> "stuck" */

        int n = snprintf(line, sizeof(line),
                         "{\"alt_ft\":%.1f,\"airspeed_kt\":%.1f,"
                         "\"vs_fpm\":%.1f,\"fault\":%s}\n",
                         reported_alt, airspeed, vs, fault ? "true" : "false");

        if (write(client_fd, line, (size_t)n) != n)
            return 0; /* client went away */

        tick++;
        nanosleep(&dt, NULL);
    }
    return 0;
}

int main(int argc, char **argv) {
    signal(SIGPIPE, SIG_IGN); /* don't die when a client disconnects */
    signal(SIGINT, on_sigint);
    srand((unsigned)time(NULL));

    int port = resolve_port(argc, argv);

    int listen_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (listen_fd < 0) {
        perror("socket");
        return 1;
    }
    int yes = 1;
    setsockopt(listen_fd, SOL_SOCKET, SO_REUSEADDR, &yes, sizeof(yes));

    struct sockaddr_in addr = {0};
    addr.sin_family = AF_INET;
    addr.sin_addr.s_addr = htonl(INADDR_ANY);
    addr.sin_port = htons((uint16_t)port);

    if (bind(listen_fd, (struct sockaddr *)&addr, sizeof(addr)) < 0) {
        perror("bind");
        return 1;
    }
    if (listen(listen_fd, 4) < 0) {
        perror("listen");
        return 1;
    }

    printf("rangeops-sim: streaming telemetry on port %d (Ctrl-C to stop)\n",
           port);
    fflush(stdout);

    while (g_running) {
        struct sockaddr_in cli;
        socklen_t clilen = sizeof(cli);
        int client_fd = accept(listen_fd, (struct sockaddr *)&cli, &clilen);
        if (client_fd < 0) {
            if (!g_running) break;
            continue;
        }
        printf("rangeops-sim: client connected from %s\n",
               inet_ntoa(cli.sin_addr));
        fflush(stdout);

        stream_flight(client_fd);
        close(client_fd);

        printf("rangeops-sim: client disconnected\n");
        fflush(stdout);
    }

    close(listen_fd);
    printf("rangeops-sim: shutdown\n");
    return 0;
}
