CC      ?= cc
CFLAGS  ?= -std=c11 -Wall -Wextra -O2
TARGET   = rangeops-sim

$(TARGET): sim.c
	$(CC) $(CFLAGS) -o $(TARGET) sim.c

run: $(TARGET)
	./$(TARGET)

clean:
	rm -f $(TARGET) *.o

.PHONY: run clean
