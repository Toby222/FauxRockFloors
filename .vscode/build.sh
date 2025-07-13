set -e

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
pushd $script_dir

configuration=${1:-Debug}

# build dll
echo "Building for RimWorld 1.5"
rm -f $mod_dir/1.5/Assemblies/*
dotnet build $script_dir/mod.csproj -c ${configuration} -p:GAME_VERSION=v1.5
echo "Building for RimWorld 1.6"
rm -f $mod_dir/1.6/Assemblies/*
dotnet build $script_dir/mod.csproj -c ${configuration} -p:GAME_VERSION=v1.6

echo "Generating About.xml"
rm -f $mod_dir/About/About.xml
xsltproc -o $mod_dir/About/About.xml $script_dir/about.xml.xslt $script_dir/mod.csproj

popd
