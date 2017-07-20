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

grammar Nesp;

expression : BRACKETLEFT WHITESPACE? list WHITESPACE? BRACKETRIGHT ;
list : (expression | string | numeric | id) (WHITESPACE? (expression | string | numeric | id))* ;
string : STRING ;
numeric : NUMERIC ;
id : ID ;

/////////////////////////////////////////////////////////////////////

// "Rule reference is not currently supported in a set"
// https://stackoverflow.com/questions/16790861/rule-reference-is-not-currently-supported-in-a-set-in-antlr4-grammar
STRING : DOUBLEQUOTE ((ESCAPE .) | ~["\\\r\n])* DOUBLEQUOTE ;

NUMERIC : (PLUS|MINUS)? [0-9]+ (PERIOD [0-9]*)? ;
ID : COMPLEXSYMBOL (WHITESPACE? GREATER WHITESPACE? ID WHITESPACE? (COMMA WHITESPACE? ID)* LESS)* ;
COMPLEXSYMBOL : SYMBOL (PERIOD SYMBOL)* ;
SYMBOL : ~[0-9"().\\ \t\r\n] (~["().\\ \t\r\n])* ;

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
