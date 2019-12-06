# Patch Syntax
The patch syntax is strongly inspired by C# syntax, with some alterations for the sake of utility, brevity, and simplicity. The biggest difference between C# syntax and taILor syntax is, that taILor puts the (return-) type of all members at the end of a member definition.
This deviation was decided on in the interest of utility. As the focus of a patch lies on a member's name, it is desirable to have it be the first bit of information in a member definition. Member types are only specified for the sake of clarity and verification.

## Encoding
Tailor requires all patch files to be UTF-8 encoded.
Invalid UTF-8 sequences are ignored during parsing. They have no effect on how a patch file is parsed.

Wherever the word "character" is used in this document, it refers to a single Unicode code point. This means that one character may end up being represented by two `char`s in C#, as .NET strings are UTF-16 encoded.

## Whitespace
Whitespace has no syntactical significance, except to separate identifiers. As such, it is only required in places where two words would otherwise be interpreted as a single identifier. This means it is mostly needed between a keyword and the name following it, like in `class Dummy`.

Here is a list of all characters Tailor considers to be whitespace:

| Codepoint | Name |
| :----- | :-- |
| `U+0009` | Horizontal Tab |
| `U+000B` | Vertical Tab |
| `U+000C` | Form Feed (FF) |
| Any in Unicode Class `Zs` | Space |


## Line Terminators
Line terminators are generally treated the same as whitespace. This means that everything in the [whitespace](#whitespace) paragraph also applies to line terminators. The only difference between them and whitespace is that they will end a line comment.

Here is a list of all characters Tailor considers to be a line terminator:

| Codepoint | Name |
| :----- | :-- |
| `U+000A` | Line Feed (LF) |
| `U+000D` | Carriage Return (CR) |
| `U+0085` | Next Line (NEL) |
| `U+2028` | Line Separator |
| `U+2029` | Paragraph Separator |

> Note: For the purposes of line numbering the character sequences `U+000D U+000A`(CRLF) and `U+000A U+000D`(LFCR) get treated as a single line terminator.


## Comments
When a `//` is encountered while parsing, the rest of the current line will be treated as a comment.
Comments have no syntactical significance. They do not affect how a patch is interpreted and will be ignored in all program operations.


## Escaping Characters
When an `@` is encountered while parsing, the next character will be interpreted as part of a name, regardless of any special function it usually serves. This can be used to represent names that contain characters which would otherwise be interpreted as control characters, like `:`, `=`, `<`, or `{`. This escaping also overrides the behaviour of the comment character `/`, the literal character `#`, and the escape character `@` itself. It can even escape whitespace.
Line breaks, however, can not be escaped and attempting to do so is a syntax error. If you do need to have a line break in an identifier, use a [UTF-16 literal](#utf-16-literals).

> Note: Escaping a character like `a`, which has no special behaviour, has the same effect as it would on special characters. The character will be interpreted literally and as part of a name.

Here are a few examples of patch snippets and the names they resolve to:

| Patch Snippet | Resolved Class Name |
| :--- | :--- |
| `class Normal` | `Normal` |
| `class Normal@!` | `Normal!` |
| `class @Normal` | `Normal` |
| `class Unspeak@<_@>able` | `Unspeak<_>able` |
| `class Name@ With@ Spaces` | `Name With Spaces` |
| `class @@Special@*` | `@Special*` |
| `class @/@/Not@ a@ comment` | `//Not a comment` |
| `class @  { }` | ` ` (a single space) |

### UTF-16 literals
When a `#` is encountered while parsing, the next four characters must represent a hexadecimal value in the range `0000`-`FFFF`. If any of those four characters is not a hexadecimal digit (`0-9`, `A-F`, `a-f`), a syntax error occurs.
A UTF-16 literal of the form `#ABCD` will produce a UTF-16 code unit (aka. `char`) with the hexadecimal value `ABCD`.

The UTF-16 literal is useful when you need to include line terminators as part of a name. It can also be used to make characters, which would otherwise be rendered in a way that is hard to discern, easier to notice.

If you encouter a symbol name that contains unmatched UTF-16 surrogates, this literal is the only way to represent that name correctly in a patch file. This is because unmatched surrogates can not be encoded as a legal UTF-8 sequence.

### Escaping keywords
A word that contains escaped characters or UTF-16 literals will never be interpreted as a keyword. For example, the following patch will be interpreted as the class `Dummy` being moved into a namespace that is literally called `default` and **not** into the default namespace.

```
namespace Before = @default
class Dummy {
    class Inner
}
```


## Renaming
### Types & Type Parameters
```
class Old = New <
    OldParam = NewParam
>
struct Old = New
enum Old = New
delegate Old = New
```

### Fields
```
class Dummy {
    OldField = NewField : int
}
```
```
enum State {
    Waiting = Idle
    Working = Busy
}
```

### Properties & Events
```
class Dummy {
    OldProperty = NewProperty { get; } : int
    OldEvent = NewEvent { add; remove; } : Action<int>
}
```

### Methods & Parameters
```
class Dummy {
    OldMethod = NewMethod (
        oldParam = newParam : int
    ) : void
}
```

## Changing Namespaces
To move a type from one namespace to another it needs to be preceeded by a `namespace` element. `namespace` is only allowed at file scope and will affect all types after it until the end of the file, or until another `namespace` element is encountered.
The following patch will move the class `A` and the struct `B` from the namespace `Before` to the namespace `After.Move`.
```
namespace Before = After.Move

class A
struct B
```
### Default Namespace
To move a type to or from the default namespace, just put `default` on that side of the `=`.
The following patch will move the class `Dummy` from the default namespace to the namespace `Somewhere`, aswell as move the class `Lost` from the namespace `Here` to the default namespace.
```
namespace default = Somewhere
class Dummy

namespace Here = default
class Lost
```
> Note: If you want to move types to a namespace that is literally called `default`, [prepend its name with an `@`](#escaping-keywords).

## Modify a Type
### Add a new field
### Add a new method
### Add a new virtual slot
### Join a getter and/or a setter into a property
### Join an adder and/or a remover into an event
### Use the type's methods to implement an interface
### Assign different methods to virtual slots
## Add new Types
## Change/Replace a method's body