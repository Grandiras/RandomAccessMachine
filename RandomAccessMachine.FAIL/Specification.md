# Scope

- List of `Statement`s

# Grammar Specification

`StatementList` -> Statement StatementList | Statement

`Statement` -> Assignment

`Assignment` -> Declaration = Expression

`Declaration` -> Type Identifier

`Type` -> int | var

`Identifier` -> [a-zA-Z_][a-zA-Z0-9_]*

`Expression` -> Number BinaryOperator Expression | Identifier BinaryOperator Expression | ( Expression ) | Number | Identifier

`BinaryOperator` -> + | - | * | /

`Number` -> [0-9]+

# Tokens

## Binary Operators

- `+` Addition
- `-` Subtraction
- `*` Multiplication
- `/` Division

## Assignment

- `=` Assignment

## Types

- `var` Variable Declaration
- `int` Integer Type

## Parentheses

- `(` Left Parenthesis
- `)` Right Parenthesis

## Identifiers
 
- `[0-9]+` Number
- `[a-zA-Z_][a-zA-Z0-9_]*` Identifier

# Examples

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

## Parentheses
```
a = (1 + 2) * 3;
```