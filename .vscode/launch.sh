set -e
set -x

script_dir=$(dirname $(readlink -f $0))
mod_dir=$(dirname $script_dir)
game_dir=$(realpath ~/.local/share/Steam/steamapps/common/RimWorld/)
pushd $mod_dir

SDL_VIDEODRIVER=X11 LC_ALL=C steam-run $game_dir/RimWorldLinux -logfile $mod_dir/log/rimworld.log -popupwindow $@

popd
