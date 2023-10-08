Precompiler for Blitz Basic. Still an early work in progress.

Features:
- Merges all 'include' references into a single file.

- Converts every macro into actual code, easier for debugging errors. '@@@' is a special keyword used to indicate the number of times a macro has been used in order to keep labels unique.

- Removes all whitespace on either end of each line, to make it less likely Blitz will complain about the length of a line.

- Enums, this code:

Enum myenum
a
b
c
End Enum

will be converted into

'#myenum_a = 0
'#myenum_b = 1
'#myenum_c = 2

- NewTypes are sorted into order of use so Blitz won't complain about a newtype being further down the program.

- Statements are sorted into order of use so Blitz won't complain about a statement being further down the program.

- Strips all comments from processed code.

- Using ;ASM on a newtype declaration will generate vector offsets for use in pure ASM, eg

NEWTYPE mytype ;asm
	fielda.w
	fieldb.l
	fieldc.w
END NEWTYPE

will generate

'#mytype_fielda = 0
'#mytype_fieldb = 2
'#mytype_fieldc = 6
