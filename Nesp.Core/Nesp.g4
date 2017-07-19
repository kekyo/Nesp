grammar Nesp;

numeric : NUMERIC ;
id : COMPLEXSYMBOL ;

NUMERIC : ('+'|'-')? ('0'..'9')+ ('.' ('0'..'9')*)? ;

COMPLEXSYMBOL : SYMBOL ('.' SYMBOL)* ;
SYMBOL : ('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')* ;
WHITESPACE : (' '|'t'|'\r'|'\n')+ -> skip ;
