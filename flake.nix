{
  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixpkgs-unstable";
    flake-parts.url = "github:hercules-ci/flake-parts";
    systems.url = "github:nix-systems/default";
    # Dev tools
    treefmt-nix = {
      url = "github:numtide/treefmt-nix";
      inputs.nixpkgs.follows = "nixpkgs";
    };
  };

  outputs =
    inputs:
    inputs.flake-parts.lib.mkFlake { inherit inputs; } {
      systems = import inputs.systems;
      imports = [ inputs.treefmt-nix.flakeModule ];
      perSystem =
        {
          config,
          self',
          pkgs,
          lib,
          system,
          ...
        }:
        {
          devShells.default = pkgs.mkShell {
            inputsFrom = [ config.treefmt.build.devShell ];
            nativeBuildInputs = with pkgs; [
              dotnetCorePackages.sdk_9_0
              nixfmt-rfc-style
              libxslt.bin
              nodePackages.npm
            ];
          };

          # Add your auto-formatters here.
          # cf. https://numtide.github.io/treefmt/
          treefmt.config = {
            projectRootFile = "flake.nix";
            programs = {
              nixfmt.enable = true;
              shfmt.enable = true;
              prettier = {
                enable = true;
                includes = [
                  "**/*.xml"
                  "**/*.xslt"
                  "**/*.json"
                  "**/*.csproj"
                  ".github/workflows/*"
                ];
                settings = {
                  plugins = [ "@prettier/plugin-xml" ];
                  bracketSameLine = true;
                  printWidth = 1000;
                  overrides = [
                    {
                      files = "*.xml";
                      options = {
                        xmlQuoteAttributes = "double";
                        xmlSortAttributesByKey = true;
                        xmlWhitespaceSensitivity = "preserve";
                      };
                    }
                  ];
                };
              };
            };
            settings.global.excludes = [
              ".direnv"
              "node_modules"
              "1.4/**/*"
              "1.5/**/*"
              "*.ase"
              "*.dll"
              "*.png"
              "About/About.xml"
            ];
          };
        };
    };
}
