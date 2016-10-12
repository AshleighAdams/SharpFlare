default: build
build: sharpflare

CSFLAGS += -langversion:experimental
#CSFLAGS += -optimize
CSFLAGS += -r:System.dll
CSFLAGS += -r:System.Numerics.dll # required by the hook system

CORE_SRC=$(wildcard ./**.cs)

sharpflare: $(CORE_SRC)
	mcs $(CSFLAGS) $^ -out:$@

clean:
	rm sharpflare || true;

.PHONY: default all clean debug install
