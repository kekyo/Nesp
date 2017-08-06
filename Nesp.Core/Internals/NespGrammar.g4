/////////////////////////////////////////////////////////////////////////////////////////////////
//
// Nesp - A Lisp-like lightweight functional language on .NET
// Copyright (c) 2017 Kouji Matsui (@kekyo2)
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//	http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
/////////////////////////////////////////////////////////////////////////////////////////////////

grammar NespGrammar;

repl : expression | list ;
expression : BRACKETLEFT WHITESPACE? list WHITESPACE? BRACKETRIGHT ;
list : (expression | string | char | numeric | id)? (WHITESPACE? (expression | string | char | numeric | id))* ;
string : STRING ;
char : CHAR ;
numeric : NUMERIC ;
id : ID ;

/////////////////////////////////////////////////////////////////////

// "Rule reference is not currently supported in a set"
// https://stackoverflow.com/questions/16790861/rule-reference-is-not-currently-supported-in-a-set-in-antlr4-grammar
STRING : DOUBLEQUOTE ((ESCAPE .) | ~["\\\r\n])* DOUBLEQUOTE ;
CHAR : QUOTE ((ESCAPE .) | ~['\\\r\n]) QUOTE ;

NUMERIC : (PLUS|MINUS)? (('0' [xX] [0-9a-fA-F]+ [uU]? [lL]?) | ([0-9]+ (([uU]? [lL]?) | (PERIOD [0-9]* [dfmDFM]?)?))) ;
ID : COMPLEXSYMBOL (WHITESPACE? GREATER WHITESPACE? ID WHITESPACE? (COMMA WHITESPACE? ID)* LESS)* ;
COMPLEXSYMBOL : SYMBOL (PERIOD SYMBOL)* ;
SYMBOL : ~[0-9"().\\ \t\r\n] (~["().\\ \t\r\n])* ;

QUOTE : '\'' ;
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
