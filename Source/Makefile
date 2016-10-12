DIRS = Core
.PHONY: all default clean install $(DIRS)

CSC ?= mcs
CSFLAGS += -langversion:experimental
#CSFLAGS += -optimize

default: sharpflare

MAIN_SRC=$(wildcard Main/**.cs)
sharpflare: SharpFlare.dll $(MAIN_SRC)
	$(CSC) $(CSFLAGS) -r:System.dll -r:SharpFlare.dll $(MAIN_SRC) -out:$@

CORE_SRC=$(wildcard Core/**.cs)
SharpFlare.dll: $(CORE_SRC)
	$(CSC) $(CSFLAGS) -t:library -r:System.dll -r:System.Numerics.dll $(CORE_SRC) -out:$@

clean:
	rm sharpflare || true
	rm *.dll || true

install: 

