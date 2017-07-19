grammar Nesp;

expression : BRACKETLEFT WHITESPACE? list WHITESPACE? BRACKETRIGHT ;
list : token (WHITESPACE? token)* ;
token : numeric | id | expression ;
numeric : NUMERIC ;
id : ID ;

/////////////////////////////////////////////////////////////////////

ID : COMPLEXSYMBOL (WHITESPACE GREATER WHITESPACE ID WHITESPACE (COMMA WHITESPACE ID)* LESS)* ;
COMPLEXSYMBOL : SYMBOL (PERIOD SYMBOL)* ;
SYMBOL : [a-zA-Z_] [a-zA-Z0-9_]* ;
NUMERIC : (PLUS|MINUS)? [0-9]+ (PERIOD [0-9]*)? ;

//DOUBLEQUOTE : '"' -> channel(HIDDEN) ;
BRACKETLEFT : '(' ;
BRACKETRIGHT : ')' ;
GREATER : '<' ;
LESS : '>' ;

ESCAPE : '\\' ;
COMMA : ',' ;
PERIOD : '.' ;
PLUS : '+' ;
MINUS : '-' ;

WHITESPACE : [ \t\r\n]+ -> channel(HIDDEN) ;
