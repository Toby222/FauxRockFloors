set -e

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
pushd $mod_dir

dotnet roslynator analyze $script_dir/mod.csproj

popd