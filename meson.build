project('SharpFlare', 'cs', version : '0.1')

# dependencies
dotnet = dependency('dotnet')

# flags
cs_flags = ['-langversion:experimental']

# compilables
libsharpflare = shared_library('SharpFlare',
	[
		'Source/Core/CLI.cs',
		'Source/Core/Hook.cs',
		'Source/Core/Logger.cs',
		'Source/Core/Plugins.cs',
		'Source/Core/RequestResponse.cs',
		'Source/Core/VirtualFileSystem.cs',
		'Source/Core/TaskPool.cs',
		'Source/Core/HttpListener.cs',
		'Source/Core/HttpRequestResponse.cs',
		'Source/Core/HttpStatus.cs',
		'Source/Core/SocketStream.cs',
	],
	install: true,
	dependencies: dotnet,
	cs_args: [cs_flags, '-r:System.Numerics.dll', '-r:System.Net.dll'],
	install_dir: get_option('libdir') + '/sharpflare',
)

if host_machine.system() == 'windows'
	exe_suffix = 'exe'
else
	exe_suffix = ''
endif

sharpflare = executable('sharpflare',
	[
		'Source/Main/Program.cs',
	],
	name_suffix: exe_suffix,
	install: true,
	dependencies: dotnet,
	link_with: libsharpflare,
	cs_args: [cs_flags],
	install_dir: get_option('libdir') + '/sharpflare',
)
