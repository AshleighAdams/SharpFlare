DIRS = Core
.PHONY: all default clean install $(DIRS)

default: $(DIRS)

$(DIRS):
	@$(MAKE) -C "$@" $(MFLAGS) $(MAKECMDGOALS)

clean: $(DIRS)
install: $(DIRS)


