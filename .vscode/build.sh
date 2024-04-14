set -e

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
pushd $script_dir

configuration=${1:-Debug}

# build dll
rm -f ../1.5/Assemblies/*
dotnet build mod.csproj -c ${configuration}

# generate About.xml
rm -f ../About/About.xml
xsltproc -o ../About/About.xml ./about.xml.xslt ./mod.csproj 

popd