# Scope

- List of `Statement`s

# Grammar Specification

`StatementList` -> Statement StatementList | Statement

`Statement` -> Command EndOfStatement | \{ StatementList \}

`Command` -> Assignment | Declaration | If | While | Break (only in loops) | Continue (only in loops)
 
`Assignment` -> Declaration = Expression

`Declaration` -> Type Identifier

`If` -> if ( Expression ) Statement else Statement | if ( Expression ) Statement

`While` -> while ( Expression ) \{ StatementList \}

`Type` -> int | var | bool

`Identifier` -> [a-zA-Z_][a-zA-Z0-9_]*

`Number` -> [0-9]+

`Expression` -> Number BinaryOperator Expression | Identifier BinaryOperator Expression | ( Expression ) BinaryOperator Expression | ( Expression ) | Number | Identifier

`BinaryOperator` -> + | - | * | / | == | != | < | > | <= | >=

`EndOfStatement` -> ;

# Tokens

## Binary Operators

- `+` Addition
- `-` Subtraction
- `*` Multiplication
- `/` Division
- `==` Equality
- `!=` Inequality
- `<` Less Than
- `>` Greater Than
- `<=` Less Than or Equal
- `>=` Greater Than or Equal

## Assignment

- `=` Assignment

## Keywords

- `var` Variable Declaration
- `int` Integer Type
- `bool` Boolean Type
- `if` If Statement
- `else` Else Statement
- `while` While Loop
- `break` Break Statement
- `continue` Continue Statement

## Parentheses

- `(` Left Parenthesis
- `)` Right Parenthesis
- `{` Left Curly Brace
- `}` Right Curly Brace
- `[` Left Square Bracket
- `]` Right Square Bracket

## Identifiers
 
- `[0-9]+` Number
- `[a-zA-Z_][a-zA-Z0-9_]*` Identifier

## End of Statement

- `;` End of Statement

# Examples

## Declaration
```
var a = 1;
```

## Assignment

```
a = 1
```

## Addition
```
a = 1 + 2
```

## Subtraction
```
a = 1 - 2
```

## Multiplication
```
a = 1 * 2
```

## Division
```
a = 1 / 2
```

## Equality
```
a = 1 == 2
```

## Inequality
```
a = 1 != 2
```

## Less Than
```
a = 1 < 2
```

## Greater Than
```
a = 1 > 2
```

## Less Than or Equal
```
a = 1 <= 2
```

## Greater Than or Equal
```
a = 1 >= 2
```

## Parentheses
```
a = (1 + 2) * 3;
```

## If Statement
```
if (a < 10) {
	a = a + 1;
} else {
	a = a - 1;
}
```

## While Loop
```
while (a < 10) {
	a = a + 1;
}
```

## Break Statement
```
while (a < 10) {
	a = a + 1;
	break;
}
```

## Continue Statement
```
while (a < 10) {
	a = a + 1;
	continue;
}
```

## Bodyless Statements
```
if (a < 10) a = a + 1;
else a = a - 1;
while (a < 10) a = a + 1;
```

## Nested Statements
```
if (a < 10) {
	if (a < 5) {
		a = a + 1;
	}
} else {
	a = a - 1;
}
```

## Array Operations
```
var a = new int[10];
a[0] = 1;
a[1] = a[0] + 1;
```

## Boolean Type
```
bool a = true;
```
