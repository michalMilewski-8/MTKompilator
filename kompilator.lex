
%using QUT.Gppg;
%namespace GardensPoint

IntNumber   ([1-9]([0-9])*)|0
RealNumber  ([1-9]([0-9])*\.([0-9])+)|(0\.([0-9])+)
Boolean     (true|false)
Ident       [a-zA-Z]([a-zA-Z0-9])*
Comment		"//".*\n
Napis		\"(\\.|[^"\\])*\"

%%

"program"     { return (int)Tokens.Program; }
"if"          { return (int)Tokens.If; }
"else"        { return (int)Tokens.Else; }
"while"       { return (int)Tokens.While; }
"read"        { return (int)Tokens.Read; }
"write"       { return (int)Tokens.Write; }
"return"      { return (int)Tokens.Return; }
"int"         { return (int)Tokens.Int; }
"double"      { return (int)Tokens.Double; }
"bool"		  { return (int)Tokens.Bool; }
"break"		  { return (int)Tokens.Break; }
"continue"    { return (int)Tokens.Continue; }
"create"      { return (int)Tokens.Create; }
{Boolean}     { yylval.val=yytext; return (int)Tokens.Boolean; }
{IntNumber}   { yylval.val=yytext; return (int)Tokens.IntNumber; }
{RealNumber}  { yylval.val=yytext; return (int)Tokens.RealNumber; }
{Ident}       { yylval.val=yytext; return (int)Tokens.Ident; }
{Comment}     { }
{Napis}		  { yylval.val=yytext; return (int)Tokens.String; }
"||"          { return (int)Tokens.LogicOr; }
"&&"          { return (int)Tokens.LogicAnd; }
"|"           { return (int)Tokens.Or; }
"&"           { return (int)Tokens.And; }
"=="          { return (int)Tokens.Equal; }
"!="          { return (int)Tokens.NotEqual; }
">"           { return (int)Tokens.Greater; }
">="          { return (int)Tokens.GreaterEqual; }
"<"           { return (int)Tokens.Smaller; }
"<="          { return (int)Tokens.SmallerEqual; }
"!"           { return (int)Tokens.LogicNot; }
"~"           { return (int)Tokens.Not; }
"="           { return (int)Tokens.Assign; }
"+"           { return (int)Tokens.Plus; }
"-"           { return (int)Tokens.Minus; }
"*"           { return (int)Tokens.Multiplies; }
"/"           { return (int)Tokens.Divides; }
"("           { return (int)Tokens.OpenPar; }
")"           { return (int)Tokens.ClosePar; }
"{"           { return (int)Tokens.OpenBlock; }
"}"           { return (int)Tokens.CloseBlock; }
"["           { return (int)Tokens.OpenIndex; }
"]"           { return (int)Tokens.CloseIndex; }
";"           { return (int)Tokens.EndLine; }
<<EOF>>       { return (int)Tokens.Eof; }
","			  { return (int)Tokens.Coma; }
" "           { }
"\t"          { }
"\n"          { Compiler.lineno++; }
"\r"          { }
