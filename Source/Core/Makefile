default: build
build: sharpflare

CSFLAGS += -optimize -langversion:experimental
CSFLAGS += /r:System.dll

CORE_SRC=$(wildcard ./**.cs)

sharpflare: $(CORE_SRC)
	mcs -out:$@ $(CSFLAGS) $^

clean:
	rm sharpflare || true;

.PHONY: default all clean debug install
