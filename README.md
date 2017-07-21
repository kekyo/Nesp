# Nesp - A Lisp-like lightweight functional language on .NET

![Nesp](Images/Nesp512.png)

## What's this?

* Nesp is a Lisp-like lightweight functional language on .NET
* Nesp is:
  * Very lightweight language syntax likely Lisp's S-expression.
    * A different topic is Nesp don't have "quote" expression.
    * Nesp expressions are lazy evaluation except literals.
  * Applicable .NET library.
  * Expandable tokens.
  * Designed for easy embedding and useful on REPL.

![Nesp (REPL)](Images/NespRepl.png)

* Still under construction...

## Nesp standard type names

* We can use C# like reserved type names (ex: int, short, string, double ...)
* Additional reserved type names (guid, datetime, timespan, type, math, enum)

## Nesp standard functions

* Numerical operators (+, -, *, /, %)
* TOOO: Declare function (define)

## Nesp REPL functions

* TOOO: Folder/file manipulations (ls, cd, mkdir)
* Clear screen (cls)
* REPL help (help)
* REPL exit (exit)

## Samples

* Basic tips:
  * Nesp REPL mode not required brackets (...).

### Literals

```
> 123
123 : byte
```

```
> 12345
12345 : short
```

```
> 1234567890
1234567890 : int
```

```
> 1234567890123456
1234567890123456 : long
```

```
> 123.456
123.456 : float
```

```
> 123.45678901234567
123.45678901234567 : double
```

```
> "abcdef"
"abcdef" : string
```

### Property reference

```
> datetime.Now
7/21/2017 12:04:43 AM : datetime
```

### Function invoke with no arguments

```
> guid.NewGuid
bb11b743-f5fe-4d68-bbe3-22e05606b3a5 : guid
```

### Function invoke with arguments

```
> int.Parse "12345"
12345 : int
```

```
> System.String.Format "ABC{0}DEF{1}GHI" 123 456.789
"ABC123DEF456.789GHI" : string
```

### Function invoke with nested invoking function expressions

* If argument is nested invoking function and it has no arguments, you aren't required brackets.
  * This sample invokes Guid.NewGuid(), but NewGuid has no arguments:

```
> string.Format "___{0}___" System.Guid.NewGuid
"___7ded117e-c873-48cf-a00b-75c57b8aa317___" : string
```

### Bind function

```
> let strrev (str) (new string (System.Linq.Enumerable.Reverse str))
strrev : string -> string
> strrev "abcdef"
"fedcba" : string
```

## License
* Copyright (c) 2017 Kouji Matsui
* Under Apache v2 http://www.apache.org/licenses/LICENSE-2.0

## History
* 0.5.1 Public open.
