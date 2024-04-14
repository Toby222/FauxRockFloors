set -e

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
pushd $mod_dir

nix fmt

popd