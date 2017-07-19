grammar Nesp;

expression : BRACKETLEFT WHITESPACE? list WHITESPACE? BRACKETRIGHT ;
list : token (WHITESPACE? token)* ;
token : expression | string | numeric | id ;
string : STRING ;
numeric : NUMERIC ;
id : ID ;

/////////////////////////////////////////////////////////////////////

ID : COMPLEXSYMBOL (WHITESPACE? GREATER WHITESPACE? ID WHITESPACE? (COMMA WHITESPACE? ID)* LESS)* ;
COMPLEXSYMBOL : SYMBOL (PERIOD SYMBOL)* ;
SYMBOL : ~[0-9"().\\ \t\r\n] (~["().\\ \t\r\n])* ;
NUMERIC : (PLUS|MINUS)? [0-9]+ (PERIOD [0-9]*)? ;

// "Rule reference is not currently supported in a set"
// https://stackoverflow.com/questions/16790861/rule-reference-is-not-currently-supported-in-a-set-in-antlr4-grammar
STRING : DOUBLEQUOTE ((ESCAPE .) | ~["\\\r\n])* DOUBLEQUOTE ;

DOUBLEQUOTE : '"' ;
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
