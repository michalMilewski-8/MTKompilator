
// Uwaga: W wywołaniu generatora gppg należy użyć opcji /gplex

%namespace GardensPoint

%union
{
public string  val;
public int  integ;
public Parser.Types type;
public Compiler.Node node;
public Dictionary<string,Compiler.DecType> idents;
public List<Compiler.Node> index;
}

//

// 

%token Program  Int Double Bool OpenBlock CloseBlock Assign Plus Minus Multiplies Divides OpenPar ClosePar EndLine Eof Error Write If Else While Read  Return LogicOr LogicAnd Or And Equal NotEqual Greater GreaterEqual Smaller SmallerEqual LogicNot Not Coma Break Continue OpenIndex CloseIndex Create
%token <val> Ident IntNumber RealNumber Boolean String

%type <type> dectype 
%type <index> indexes 
%type <node> line  expr expr2 expr3 expr4 expr5 expr6 expr1 val block code unary bit mult add rel logic
%type <idents> declarations declaration longdeclaration
%type <integ> dimmensions


%%

start     : Program OpenBlock body CloseBlock Eof
          | Program error Eof {
                    ++Compiler.errors;
                    Console.WriteLine("line {0} error: Syntax error ",0);
                    YYABORT;
                    }
          |OpenBlock body CloseBlock Eof {
                    ++Compiler.errors;
                    Console.WriteLine("line {0} error: Syntax error ",0);
                    YYABORT;
                    }
          | Eof {
                    ++Compiler.errors;
                    Console.WriteLine("line {0} error: Syntax error ",0);
                    YYABORT;
                    }

          ;

body      : declarations code { Compiler.treeRoot = $2; ((Compiler.CodeNode)$2).idents = $1; ((Compiler.CodeNode)$2).codeId = "super"; }
          | declarations { Compiler.treeRoot = new Compiler.CodeNode(); ((Compiler.CodeNode)Compiler.treeRoot).idents = $1;}
          | code { Compiler.treeRoot = $1; ((Compiler.CodeNode)$1).codeId = "super"; }
          |  {Compiler.treeRoot = new Compiler.EmptyNode();}
          ;

declarations : declaration declarations {
                    $$ = $1;
                    foreach(var pair in $2)
                    {
                        if ($$.ContainsKey(pair.Key))
                        {
                             Console.WriteLine("line: {0} error: such id already exists",Compiler.lineno);
                             ++Compiler.errors;
                        }
                        else
                            $$.Add(pair.Key, pair.Value);
                    }
                }
             | declaration {$$ = $1;}

             ;

declaration  : dectype
                {
                    Compiler.currentDecType = new Compiler.DecType($1);
                }
               Ident longdeclaration end 
                { 
                    if ($4.ContainsKey($3))
                    {
                     Console.WriteLine("line: {0} error: such id already exists",Compiler.lineno);
                     ++Compiler.errors;
                     $$ = new Dictionary<string,Compiler.DecType>();
                    }
                    else
                       $4.Add($3, Compiler.currentDecType);
                    $$ = $4;
                }
             | dectype OpenIndex dimmensions CloseIndex
                {
                    Compiler.currentDecType = new Compiler.DecType($1,true,$3);
                }
               Ident longdeclaration end 
                { 
                    if ($7.ContainsKey($6))
                    {
                     Console.WriteLine("line: {0} error: such id already exists",Compiler.lineno);
                     ++Compiler.errors;
                     $$ = new Dictionary<string,Compiler.DecType>();
                    }
                    else
                       $7.Add($6, Compiler.currentDecType);
                    $$ = $7;
                }
             | dectype error end 
                {
                    Console.WriteLine("line {0,3} error: syntax error. ",Compiler.lineno);
                    ++Compiler.errors; 
                    $$ = new Dictionary<string,Compiler.DecType>();
                }
            ;

dimmensions : Coma dimmensions{ $$ = $2+1;}
            | {$$ = 1;}
            ;

longdeclaration     : Coma Ident longdeclaration
                        { 
                            if ($3.ContainsKey($2))
                            {
                             Console.WriteLine("line: {0} error: such id already exists",Compiler.lineno);
                             ++Compiler.errors;
                            }
                            else
                               $3.Add($2, Compiler.currentDecType);

                            $$ = $3;
                        }
                    | {$$ = new Dictionary<string,Compiler.DecType>();}
                    ;
             

dectype      : Int {$$ = Types.IntegerType;}
             | Double {$$ = Types.DoubleType;}
             | Bool {$$ =Types.BooleanType;}
             | error Ident end 
                { 
                    Console.WriteLine("line {0,3} error: syntax error. ",Compiler.lineno);
                    ++Compiler.errors;
                    $$ = Types.NoneType;
                }
             ;

code      : block code { 
                Compiler.CodeNode node = new Compiler.CodeNode();
                node.inside.Add($1);
                Compiler.CodeNode child = $2 as Compiler.CodeNode;
                if (child is null) {
                    ++Compiler.errors;
                    Console.WriteLine("line {0} error: Syntax error",0);
                }
                else {
                    node.inside.AddRange(child.inside);
                }
                $$ = node;
                $$.linenumber = Compiler.lineno;
            }
          | block { 
                Compiler.CodeNode node = new Compiler.CodeNode();
                node.inside.Add($1);
                $$ = node;
                $$.linenumber = Compiler.lineno;
            }
          ;

block     : OpenBlock code CloseBlock { $$ = new Compiler.BlockNode($2); $$.linenumber = Compiler.lineno;}
          | OpenBlock declarations code CloseBlock
          {
            ((Compiler.CodeNode)$3).idents = $2;
            $$ = new Compiler.BlockNode($3);
            $$.linenumber = Compiler.lineno;
          } 
          | OpenBlock declarations CloseBlock { $$ = new Compiler.BlockNode(new Compiler.EmptyNode()); $$.linenumber = Compiler.lineno;}
          | line { $$ = new Compiler.BlockNode($1);$$.linenumber = Compiler.lineno; }
          | OpenBlock CloseBlock { $$ = new Compiler.BlockNode(new Compiler.EmptyNode()); $$.linenumber = Compiler.lineno;}
          ;

line      : expr end { $$ = new Compiler.BareExpresionNode($1);$$.linenumber = Compiler.lineno; } 
          | Read Ident end 
            { 
                Compiler.IdentNode node = new Compiler.IdentNode($2); 
                 node.linenumber = Compiler.lineno;
                $$ = new Compiler.ReadNode(node);
                $$.linenumber = Compiler.lineno;
            }
          | Read Ident OpenIndex expr indexes CloseIndex end 
            {
                $5.Insert(0,$4);
                Compiler.TabIdentNode node = new Compiler.TabIdentNode($2,$5); 
                node.linenumber = Compiler.lineno;
                $$ = new Compiler.ReadNode(node);
                $$.linenumber = Compiler.lineno; 
            }
          | Write expr end { $$ = new Compiler.WriteNode($2);$$.linenumber = Compiler.lineno; }
          | Write String end { $$ = new Compiler.WriteNode(new Compiler.StringNode($2)); $$.linenumber = Compiler.lineno;}
          | If OpenPar expr ClosePar block 
                {
                  $$ = new Compiler.IfNode($3,$5);
                  $$.linenumber = Compiler.lineno;
                }
          | If OpenPar expr ClosePar block Else block 
                {
                  $$ = new Compiler.IfElseNode($3,$5,$7);
                  $$.linenumber = Compiler.lineno;
                }
          | While OpenPar expr ClosePar block 
                {
                  $$ = new Compiler.WhileNode($3,$5, Compiler.lineno);
                }
          | Return end
                {
                  $$ = new Compiler.ReturnNode();
                  $$.linenumber = Compiler.lineno;
                }
          | Continue end 
                {
                  $$ = new Compiler.ContinueNode();
                  $$.linenumber = Compiler.lineno;
                }
          | Break end 
                {
                  $$ = new Compiler.BreakNode(Compiler.lineno);
                }
          | Break IntNumber end 
                {
                  $$ = new Compiler.BreakNode(new Compiler.IntNumberNode($2),Compiler.lineno);
                }
          | Create Ident OpenIndex expr indexes CloseIndex end 
                {
                    $5.Insert(0,$4);
                    $$ = new Compiler.TabIdentNode($2,$5,true); 
                    $$.linenumber = Compiler.lineno;
                }
          | EndLine {$$ = new Compiler.EmptyNode(); Console.WriteLine("line {0,3} error: syntax error ",Compiler.lineno);  ++Compiler.errors; ;} 
          | error EndLine 
                {
                    $$ = new Compiler.EmptyNode();
                  Console.WriteLine("line {0,3} error: syntax error ",Compiler.lineno);  ++Compiler.errors; ;
                }
          | error Eof 
                {
                $$ = new Compiler.EmptyNode();
                  Console.WriteLine("line {0,3} error: unexpected Eof ",Compiler.lineno);  ++Compiler.errors; 
                  YYABORT; 
                }
          | Eof 
                {
                $$ = new Compiler.EmptyNode();
                    ++Compiler.errors;
                    Console.WriteLine("line {0,3} error: unexpected Eof",0);
                    YYABORT;
                }
          ;


expr      : Ident Assign expr {
                Compiler.IdentNode node = new Compiler.IdentNode($1);
                node.linenumber = Compiler.lineno;
                $$ = new Compiler.AssignNode(node, $3);
                $$.linenumber = Compiler.lineno;
            }
          | Ident OpenIndex expr indexes CloseIndex Assign expr {
                $4.Insert(0,$3);
                Compiler.TabIdentNode node = new Compiler.TabIdentNode($1,$4); 
                node.linenumber = Compiler.lineno;
                $$ = new Compiler.AssignNode(node, $7);
                $$.linenumber = Compiler.lineno;
            }
          | expr1 {$$ = $1;}
          ;

expr1     : expr1 logic expr2 { 
                Compiler.LogicNode node = $2 as  Compiler.LogicNode;
                if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $3;
                    node.left = $1;
                }
                $$ = node;
            }
          | expr2 {$$ = $1;}
          ;

expr2     : expr2 rel expr3 { 
                Compiler.RelationsNode node = $2 as Compiler.RelationsNode;
                if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $3;
                    node.left = $1;
                }
                $$ = node;
            }
          | expr3 {$$ = $1;}
          ;

expr3     : expr3 add expr4 { 
                Compiler.AdditiveNode node = $2 as Compiler.AdditiveNode;
                if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $3;
                    node.left = $1;
                }
                $$ = node;
            }
          | expr4 {$$ = $1;}
          ;

expr4     : expr4 mult expr5 { 
                Compiler.MultiplNode node = $2 as Compiler.MultiplNode;
                if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $3;
                    node.left = $1;
                }
                $$ = node;
            }
          | expr5 {$$ = $1;}
          ;

expr5     : expr5 bit expr6 { 
                Compiler.BitNode node = $2 as Compiler.BitNode;
                if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $3;
                    node.left = $1;
                }
                $$ = node;
            }
          | expr6 {$$ = $1;}
          ;

expr6     : unary expr6 { 
               Compiler.UnaryNode node = $1 as Compiler.UnaryNode;
               if(node is null) 
                {
                    ++Compiler.errors;
                    Console.WriteLine("line: {0} syntax error",Compiler.lineno);
                }
                else
                {
                    node.right = $2;
                }
                $$ = node;
                $$.linenumber = Compiler.lineno;
            }
          | val {$$ = $1; }
          ;

logic       : LogicAnd {$$ = new Compiler.LogicAndNode();  $$.linenumber = Compiler.lineno;}
            | LogicOr {$$ = new Compiler.LogicOrNode();  $$.linenumber = Compiler.lineno;}
            ;

rel         : Equal {$$ = new Compiler.EqualNode();  $$.linenumber = Compiler.lineno;}
            | NotEqual {$$ = new Compiler.NotEqualNode();  $$.linenumber = Compiler.lineno;}
            | Greater {$$ = new Compiler.GreaterNode();  $$.linenumber = Compiler.lineno;}
            | GreaterEqual {$$ = new Compiler.GreaterEqualNode();  $$.linenumber = Compiler.lineno;}
            | Smaller {$$ = new Compiler.SmallerNode();  $$.linenumber = Compiler.lineno;}
            | SmallerEqual {$$ = new Compiler.SmallerEqualNode();  $$.linenumber = Compiler.lineno;}
            ;

add         : Plus {$$ = new Compiler.PlusNode();  $$.linenumber = Compiler.lineno;}
            | Minus {$$ = new Compiler.MinusNode();  $$.linenumber = Compiler.lineno;}
            ;

mult        : Multiplies {$$ = new Compiler.MultNode();  $$.linenumber = Compiler.lineno;}
            | Divides {$$ = new Compiler.DivNode();  $$.linenumber = Compiler.lineno;}
            ;

bit         : Or  {$$ = new Compiler.OrNode();  $$.linenumber = Compiler.lineno;}
            | And {$$ = new Compiler.AndNode();  $$.linenumber = Compiler.lineno;}
            ;

unary       : LogicNot  {$$ = new Compiler.LogicNotNode();  $$.linenumber = Compiler.lineno;}
            | Not {$$ = new Compiler.NotNode();  $$.linenumber = Compiler.lineno;}
            | Minus {$$ = new Compiler.UnaryMinusNode();  $$.linenumber = Compiler.lineno;}
            | OpenPar Int ClosePar {$$ = new Compiler.IntCastNode();  $$.linenumber = Compiler.lineno;}
            | OpenPar Double ClosePar {$$ = new Compiler.DoubleCastNode();  $$.linenumber = Compiler.lineno;}
            ;

val         : Ident {$$ = new Compiler.IdentNode($1);  $$.linenumber = Compiler.lineno;}
            | IntNumber {$$ = new Compiler.IntNumberNode($1);  $$.linenumber = Compiler.lineno;}
            | RealNumber {$$ = new Compiler.DoubleNumberNode($1);  $$.linenumber = Compiler.lineno;}
            | Boolean {$$ = new Compiler.BooleanNode($1);  $$.linenumber = Compiler.lineno;}
            | Ident OpenIndex expr indexes CloseIndex
                {
                    $4.Insert(0,$3);
                    $$ = new Compiler.TabIdentNode($1,$4); 
                    $$.linenumber = Compiler.lineno;
                }
            | OpenPar expr ClosePar {$$ = $2;}
            ;

indexes     : Coma expr indexes 
              {
                $$ = $3;
                $$.Insert(0,$2);
              }
            | {$$ = new List<Compiler.Node>();}
            ;

end       : EndLine
          | Eof {
               Console.WriteLine("  line {0,3}:  syntax error - unexpected symbol Eof",Compiler.lineno);
               ++Compiler.errors;
               YYABORT;
               }
          ;


%%

public enum Types{
    NoneType = 0,
    IntegerType = 1,
    DoubleType = 2,
    BooleanType = 3,
    StringType = 4
}

public Parser(Scanner scanner) : base(scanner) { }

