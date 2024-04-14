set -e

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
pushd $script_dir

SDL_VIDEODRIVER=X11 LC_ALL=C steam-run ~/.local/share/Steam/steamapps/common/RimWorld/RimWorldLinux -logfile $mod_dir/log/rimworld.log -popupwindow $@

popd