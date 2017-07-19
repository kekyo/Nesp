grammar Nesp;

expression : bracketLeft whiteSpace? list whiteSpace? bracketRight ;
list : token (whiteSpace? token)* ;
token : numeric | id | expression ;
numeric : NUMERIC ;
id : COMPLEXSYMBOL ;

bracketLeft : BRACKETLEFT ;
bracketRight : BRACKETRIGHT ;
whiteSpace : WHITESPACE ;

/////////////////////////////////////////////////////////////////////

NUMERIC : ('+'|'-')? ('0'..'9')+ ('.' ('0'..'9')*)? ;
COMPLEXSYMBOL : SYMBOL ('.' SYMBOL)* ;
SYMBOL : ('a'..'z'|'A'..'Z'|'_') ('a'..'z'|'A'..'Z'|'0'..'9'|'_')* ;

BRACKETLEFT : '(' ;
BRACKETRIGHT : ')' ;

WHITESPACE : (' '|'\t'|'\r'|'\n')+ ;
