# ILTailor
ILTailor is a tool that allows you to modify .NET assemblies and save those modifications as separate patch files. Those patches can then be applied to copies of that assembly without having to redistribute the assembly itself.

The main application of this tool is in scenarios where you want to distribute modifications to a .NET assembly, but are unable to distribute the modified assembly itself. This is especially the case when modding games, as redistributing a game's files is usually prohibited.

## Features
- [ ] **Creating Patches**
- [ ] **Applying Patches**
  - [ ] Change symbol names:
    - [ ] Types
    - [ ] Type Parameters
    - [ ] Methods
    - [ ] Parameters
    - [ ] Fields
    - [ ] Properties
    - [ ] Events
  - [ ] Move Types into different namespaces
  - [ ] Convert a getter and/or a setter into a Property
  - [ ] Add new members to a Type
  - [ ] Add new Types
  - [ ] Change/Replace a method's body
- [ ] **Combining Patches**
  - [ ] Merge multiple patches into one
  - [ ] Chain multiple patches together
- [ ] **Formatting Patches**
  - [ ] Fix indentation
  - [ ] Insert line breaks
  - [ ] Add/Remove type information

### Creating Patches
This is more of a convenience feature. TaILor can produce a patch that lists all the symbols in an assembly but doesn't rename any of them. The purpose of such an identity patch is to give you a base to work off of, without requiring you to type out a ton of patch file boilerplate.

### Applying Patches
This is the main feature, I should probably write something here.

### Combining Patches
There are two methods for combining patches: merging, and chaining.
To illustrate their differences, we will consider the following example patches:

```
#A
class Foo = Bar
class Dummy = Example
```

```
#B
class Bar = Baz
class Placeholder = Dummy
```

#### Merging
Merging produces a patch that combines the information of multiple patches into one. It also reports conflicts where more than one of the inputs tries to change the same symbol name, and offers multiple ways of resolving those conflicts. Merging is useful, if you want to combine patches that were created by multiple people into one.

The result of merging `A` and `B` wold be
```
class Foo = Bar
class Dummy = Example
class Bar = Baz
class Placeholder = Dummy
```

#### Chaining
Chaining produces a patch that has the same effect as applying all specified patches in sequence. This is useful if you want to split your patches into multiple seperate steps for development, but want to have one patch to give to users. It is also useful when you want to support multiple versions of an assembly whilst minimizing duplicate code. You can have seperate patches for each version, which all convert to the same intermedeate representation, and then have one patch that converts the intermedeate representation to the desired output.

The result of chaining `A` and `B` depends on their order,
if you chain `A` then `B` the result will be
```
class Foo=Baz
class Dummy = Example
class Placeholder = Dummy
```
but if you chain `B` then `A`, the result will be
```
class Bar = Baz
class Placeholder = Example
class Foo = Bar
```

### Formatting Patches
Another convenience feature. This one will simply parse a patch and then emit an equivalent patch with the specified formatting applied.


## Patch File Format
TaILor's patches are plaintext files. They are inteded to be easily written and read by humans, without the need for any special editor.
The patch syntax is strongly inspired by C# syntax, with some alterations for the sake of utility, brevity, and simplicity. The biggest difference between C# syntax and taILor syntax is, that taILor puts the (return-) type of all members at the end of a member definition. This change was made for the sake of utility. As the focus of the patch files lies on a member's name, it is desirable to have it be the first bit of information in a member definition. Member types are only specified for the sake of clarity and verification, and may be omitted if desired.


## How to use TaILor
### Control formatting
Flags that change how the output patch files are formatted.

### Control trimming
Flags that control what kinds of symbols will get omitted when producing patches.


## Limited Scope
Note, that there are currently no plans to add features which would require code to be rewritten implicitly. This is because I do not want to add features which would likely render an assembly invalid unless special care is taken by the user. This includes things like adding parameters to, or removing them from, a method, to give an example.
