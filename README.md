# ADR-Cli

ADR is a command-line tool that is used to create and maintain `Architecture Decision Records` as described by [Michael Nygård](https://cognitect.com/authors/MichaelNygard.html) in [The following blog article](https://cognitect.com/blog/2011/11/15/documenting-architecture-decisions).

![SimpleAnimation](Docs/Images/adrTool.gif)

## Versions

| Version | Description |
| ------- | ----------- | 
| 1.0.2   | Add quickmode actions `new` and `open` |
| 1.0.1   | Add support for superseding an ADR |
| 1.0.0   | Initial Version, full of bugs :) |


## Installation

The tool is built and packaged as a `DotNet Tool`. If you just want to install it, then type the following into your console of choice: 

```sh
> dotnet tool install -g ADR-Cli --version 1.0.2
```

Or to update:

```sh
> dotnet tool update -g ADR-Cli --version 1.0.2
```

Which should hook you up with the latest version. 

## Warning: Slightly opinionated!
ADR is slightly opinionated in that:
- It requires that you have [VS Code](https://code.visualstudio.com/) installed on your system
- It requires that you are using [Git](https://git-scm.com/) as your version control system
- It assumes that you want your ADRs saved to your `repository root folder` + `/docs/adr`
- It requires that the index file is named `0000-index.md` (it will create this file when required)

If you can live with these requirements and assumptions, then you're set. 

## Usage

To invoke the command, just type `adr`, or if you want to manage ADRs in a different folder than your current directory, just provide an absolute or relative path to a folder where ADRs exist.

```sh
> adr ./code/myProject
```

> **NOTE**<br />
> ADR will provide an error message if you run it "outside" a git repository folder. However, it does not matter how deep into a repository folder structure you are, it will figure out where to find the docs/adr once started. 

### Initial run
If this is the first time you're using ADR, and you run the tool, you will be asked whether you would like `adr` to create the `docs/adr` folder for you. Once you confirm, `adr` will create the folder, and also an index file and your first ADR entry (pointing to the decision of using ADRs, to begin with). 

### Normal operation

Once you have your initial folder and files in place, `adr`, when invoked, will show you a table over all your ADRs so far in that repository, and provides you with a menu to perform some simple operations: 
- Arrow up/down to highlight an ADR. **[ENTER]** opens it in VS Code.
- Pressing `A` creates a new ADR, prompting you for a title first, then it opens up a template in VS Code
- Pressing `R` renames the currently highlighted ADR
- Pressing `S` will create a new ADR that supersedes (overrules) the currently selected one in the table)
- Pressing `O` opens the entire ADR folder in VS Code. Handy if you need to multi-edit
- Pressing `I` will recreate the `0000-index.md` file based on the adr folder contents

After every "command", the `adr` tool exits. It does not return to the menu. 

> **NOTE** <br />
> It is not possible to `delete` an ADR using the tool. This is because ADRs are supposed to be a forward-only record of important decisions. However, nothing prevents you from fiddling with the files in this folder. This is not rocket science :)

### Syntax

ADR has very few parameters: 

```sh
> adr
# with no parameters, assumes the current directory as the operating directory

> adr .
> adr ./code/someRepo/src/
# Accepts relative paths

> adr C:\dev\work\AdrTool\Adr
# Also accepts complete paths

> adr --help
# Should display version and a very rudimentary instruction
```

### Introduced in v1.0.2: Quick Actions
Once you're comfortable with `adr` you may want to speed things up. Version `1.0.2` introduces the concept of `quickmode commands`. Currently, these are: 

### `adr new <title> [path]`
Creates a new ADR document without a confirmation prompt. 

```sh
# title: Required. The title of the new ADR to add
# path: Optional. Absolute or relative path to your project folder
> adr new "Prefer Azure ServiceBus for cross-service messaging"
```

### `adr open [path]`
Open up your ADR folder in `VS Code`

```sh
# path: Optional. Absolute or relative path to your project folder
# Example:
> adr open
> adr open .
> adr open /mnt/e/dev/work/myproject
```







