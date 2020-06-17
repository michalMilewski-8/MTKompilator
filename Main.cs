
using System;
using System.IO;
using System.Collections.Generic;
using GardensPoint;
using System.Globalization;

public class Compiler
{

    public static int errors = 0;

    public static List<string> source;

    // arg[0] określa plik źródłowy
    // pozostałe argumenty są ignorowane
    public static int Main(string[] args)
    {
        string file;
        FileStream source;
        Console.WriteLine("\nCIL Code Generator for mini language - Gardens Point");
        if (args.Length >= 1)
            file = args[0];
        else
        {
            Console.Write("\nsource file:  ");
            file = Console.ReadLine();
        }
        try
        {
            var sr = new StreamReader(file);
            string str = sr.ReadToEnd();
            sr.Close();
            Compiler.source = new System.Collections.Generic.List<string>(str.Split(new string[] { "\r\n" }, System.StringSplitOptions.None));
            source = new FileStream(file, FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine("\n" + e.Message);
            return 1;
        }
        Scanner scanner = new Scanner(source);
        Parser parser = new Parser(scanner);
        Console.WriteLine();
        sw = new StreamWriter(file + ".il");
        parser.Parse();
        if(treeRoot is null)
        {
            errors++;
            Console.WriteLine($"line 0 error: syntax error");
        }
        if (errors == 0)
        {
            GenProlog();
            treeRoot?.GenIdents();
            treeRoot?.SetRefParentCodeNode(null);
            treeRoot?.GenCode();
            GenEpilog();
        }
        sw.Close();
        source.Close();
        if (errors == 0)
            Console.WriteLine("  compilation successful\n");
        else
        {
            Console.WriteLine($"\n  {errors} errors detected\n");
            File.Delete(file + ".il");
        }
        return errors == 0 ? 0 : 2;
    }

    public static Node treeRoot = null;
    public static DecType currentDecType;
    public static int maxStack = 1;

    public struct DecType
    {
        public DecType(Parser.Types t = Parser.Types.NoneType, bool table = false, int index = 1)
        {
            type = t;
            isTable = table;
            tableIndexesNum = index;
        }
        public Parser.Types type;
        public bool isTable;
        public int tableIndexesNum;
    }

    private static int blockIdCounter = 0;
    private static int codeIdCounter = 0;
    private static int shortLogicCounter = 0;
    public static int lineno = 1;
    public abstract class Node
    {
        public int linenumber;
        public Node refForWhile = null;
        public Node refParentCodeNode = null;

        public virtual void SetRefForWhile(Node whileRef) { refForWhile = whileRef; }
        public virtual void SetRefParentCodeNode(Node parentCodeNode) { refParentCodeNode = parentCodeNode; }
        public virtual void GenIdents() { }

        public virtual int GetStackDepth() { return 1; }
        public abstract string GenCode();
        public abstract Parser.Types CheckType();
    }
    public class EmptyNode : Node
    {
        public EmptyNode() { }
        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            return null;
        }
    }
    public class BareExpresionNode : Node
    {
        public Node expr;
        public BareExpresionNode() { }
        public BareExpresionNode(Node e) { expr = e; }
        public override Parser.Types CheckType()
        {
            return expr.CheckType();
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                expr.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override int GetStackDepth()
        {
            return expr.GetStackDepth();
        }
        public override string GenCode()
        {
            expr.GenCode();
            EmitCode("pop");
            return null;
        }
    }
    public class DoubleCastNode : UnaryNode
    {
        public DoubleCastNode() { }
        public DoubleCastNode(Node r) : base(r)
        {
        }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            right.GenCode();
            EmitCode("conv.r8");
            return null;
        }

        public override Parser.Types CheckType()
        {
            if (right.CheckType() == Parser.Types.NoneType || right.CheckType() == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return Parser.Types.DoubleType;
        }
    }
    public class IntCastNode : UnaryNode
    {
        public IntCastNode(Node r) : base(r)
        {
        }
        public IntCastNode() { }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            right.GenCode();
            EmitCode("conv.i4");
            return null;
        }

        public override Parser.Types CheckType()
        {
            if (right.CheckType() == Parser.Types.NoneType || right.CheckType() == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return Parser.Types.IntegerType;
        }
    }
    public class UnaryNode : Node
    {
        public Node right;
        public UnaryNode() { }
        public UnaryNode(Node r)
        {
            right = r;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override string GenCode()
        {
            throw new NotImplementedException();
        }

        public override int GetStackDepth()
        {
            return right.GetStackDepth() + 1;
        }

        public override Parser.Types CheckType()
        {
            throw new NotImplementedException();
        }
    }
    public class UnaryMinusNode : UnaryNode
    {
        public UnaryMinusNode(Node r) : base(r)
        {
        }
        public UnaryMinusNode() { }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            right.GenCode();
            EmitCode("neg");
            return null;
        }

        public override Parser.Types CheckType()
        {
            var type = right.CheckType();
            if (type != Parser.Types.IntegerType && type != Parser.Types.DoubleType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return type;
        }
    }
    public class NotNode : UnaryNode
    {
        public NotNode() { }
        public NotNode(Node r) : base(r) { }


        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            right.GenCode();
            EmitCode("not");
            return null;
        }
        public override Parser.Types CheckType()
        {
            if (right.CheckType() != Parser.Types.IntegerType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return Parser.Types.IntegerType;
        }

    }
    public class LogicNotNode : UnaryNode
    {
        public LogicNotNode() { }
        public LogicNotNode(Node r) : base(r)
        {
        }

        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            right.GenCode();
            EmitCode("ldc.i4.0");
            EmitCode("ceq");
            return null;
        }


        public override Parser.Types CheckType()
        {
            if (right.CheckType() != Parser.Types.BooleanType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return Parser.Types.BooleanType;
        }
    }
    public class BitNode : Node
    {
        public Node left;
        public Node right;
        public BitNode() { }
        public BitNode(Node _left, Node _right)
        {
            left = _left;
            right = _right;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                left.SetRefParentCodeNode(parentCodeNode);
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override int GetStackDepth()
        {
            int r = right.GetStackDepth(), l = left.GetStackDepth();

            return (r>l?r:l)+1;
        }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
                return null;
            left.GenCode();
            right.GenCode();
            return null;
        }

        public override Parser.Types CheckType()
        {
            if (left.CheckType() != Parser.Types.IntegerType || right.CheckType() != Parser.Types.IntegerType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            return Parser.Types.IntegerType;
        }
    }
    public class AndNode : BitNode
    {
        public AndNode() { }
        public AndNode(Node _left, Node _right) : base(_left, _right) { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("and");
            return null;
        }

    }
    public class OrNode : BitNode
    {
        public OrNode() { }
        public OrNode(Node _left, Node _right) : base(_left, _right) { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("or");
            return null;
        }
    }
    public class RelationsNode : Node
    {
        public Node left;
        public Node right;

        public RelationsNode(Node _left, Node _right)
        {
            left = _left;
            right = _right;
        }
        public RelationsNode() { }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                left.SetRefParentCodeNode(parentCodeNode);
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override int GetStackDepth()
        {
            int r = right.GetStackDepth(), l = left.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override string GenCode()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return null;
            var left_t = left.CheckType();
            var right_t = right.CheckType();
            type = left_t == right_t ? right_t : Parser.Types.DoubleType;
            left.GenCode();
            if (left.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            right.GenCode();
            if (right.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            return null;

        }
        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type == Parser.Types.BooleanType || left_type == Parser.Types.NoneType || left_type == Parser.Types.StringType ||
                right_type == Parser.Types.BooleanType || right_type == Parser.Types.NoneType || right_type == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else
            {
                return Parser.Types.BooleanType;
            }
        }
    }
    public class SmallerEqualNode : RelationsNode
    {
        public SmallerEqualNode(Node _left, Node _right) : base(_left, _right) { }
        public SmallerEqualNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("cgt ");
            EmitCode("ldc.i4.0");
            EmitCode("ceq");
            return null;
        }
    }
    public class SmallerNode : RelationsNode
    {
        public SmallerNode(Node _left, Node _right) : base(_left, _right) { }
        public SmallerNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("clt");
            return null;
        }
    }
    public class GreaterEqualNode : RelationsNode
    {
        public GreaterEqualNode(Node _left, Node _right) : base(_left, _right) { }
        public GreaterEqualNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("clt");
            EmitCode("ldc.i4.0");
            EmitCode("ceq");
            return null;
        }
    }
    public class GreaterNode : RelationsNode
    {
        public GreaterNode(Node _left, Node _right) : base(_left, _right) { }
        public GreaterNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("cgt ");
            return null;
        }
    }
    public class NotEqualNode : RelationsNode
    {
        public NotEqualNode(Node _left, Node _right) : base(_left, _right) { }
        public NotEqualNode() { }
        public override string GenCode()
        {
            var tmp2 = new EqualNode(left, right);
            tmp2.linenumber = linenumber;
            Node tmp = new LogicNotNode(tmp2);
            tmp.linenumber = linenumber;
            tmp.GenCode();
            return null;
        }
        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type == Parser.Types.NoneType || left_type == Parser.Types.StringType ||
                right_type == Parser.Types.NoneType || right_type == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else if (left_type != right_type && (left_type == Parser.Types.BooleanType || right_type == Parser.Types.BooleanType))
            {
                return Parser.Types.NoneType;
            }
            else
            {
                return Parser.Types.BooleanType;
            }
        }
    }
    public class EqualNode : RelationsNode
    {
        public EqualNode() { }
        public EqualNode(Node _left, Node _right) : base(_left, _right) { }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
            {
                return null;
            }
            var left_t = left.CheckType();
            var right_t = right.CheckType();
            var type = left_t == right_t ? right_t : Parser.Types.DoubleType;
            left.GenCode();
            if (left.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            right.GenCode();
            if (right.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            EmitCode("ceq");
            return null;
        }

        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type == Parser.Types.NoneType || left_type == Parser.Types.StringType ||
                right_type == Parser.Types.NoneType || right_type == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else if (left_type != right_type && (left_type == Parser.Types.BooleanType || right_type == Parser.Types.BooleanType))
            {
                return Parser.Types.NoneType;
            }
            else
            {
                return Parser.Types.BooleanType;
            }
        }
    }
    public class LogicNode : Node
    {
        public Node left;
        public Node right;
        public LogicNode() { }
        public LogicNode(Node _left, Node _right)
        {
            left = _left;
            right = _right;
        }
        public override int GetStackDepth()
        {
            int r = right.GetStackDepth(), l = left.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override string GenCode()
        {
            return null;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                left.SetRefParentCodeNode(parentCodeNode);
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type != Parser.Types.BooleanType || right_type != Parser.Types.BooleanType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else
            {
                return Parser.Types.BooleanType;
            }
        }
    }
    public class LogicOrNode : LogicNode
    {
        public LogicOrNode(Node _left, Node _right) : base(_left, _right) { }
        public LogicOrNode() { }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
            {
                return null;
            }
            string ommit_part_e = $"ope_{shortLogicCounter++}";
            string not_ommit_part_e = $"ope_{shortLogicCounter++}";
            left.GenCode();
            EmitCode($"brtrue {ommit_part_e}");
            right.GenCode();
            EmitCode($"br {not_ommit_part_e}");
            EmitCode($"{ommit_part_e}: ldc.i4.1");
            EmitCode($"{not_ommit_part_e}: nop");
            return null;
        }
    }
    public class LogicAndNode : LogicNode
    {
        public LogicAndNode(Node _left, Node _right) : base(_left, _right) { }
        public LogicAndNode() { }
        public override string GenCode()
        {
            if (this.CheckType() == Parser.Types.NoneType)
            {
                return null;
            }
            string ommit_part_e = $"ope_{shortLogicCounter++}";
            string not_ommit_part_e = $"ope_{shortLogicCounter++}";
            left.GenCode();
            EmitCode($"brfalse {ommit_part_e}");
            right.GenCode();
            EmitCode($"br {not_ommit_part_e}");
            EmitCode($"{ommit_part_e}: ldc.i4.0");
            EmitCode($"{not_ommit_part_e}: nop");
            return null;
        }

    }
    public class MultiplNode : Node
    {
        public Node left;
        public Node right;

        public MultiplNode(Node _left, Node _right)
        {
            left = _left;
            right = _right;
        }
        public MultiplNode() { }
        public override string GenCode()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return null;
            left.GenCode();
            if (left.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            right.GenCode();
            if (right.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            return null;

        }
        public override int GetStackDepth()
        {
            int r = right.GetStackDepth(), l = left.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                left.SetRefParentCodeNode(parentCodeNode);
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type == Parser.Types.BooleanType || left_type == Parser.Types.NoneType || left_type == Parser.Types.StringType ||
                right_type == Parser.Types.BooleanType || right_type == Parser.Types.NoneType || right_type == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else if (left_type == Parser.Types.DoubleType || right_type == Parser.Types.DoubleType)
            {
                return Parser.Types.DoubleType;
            }
            else
            {
                return left_type;
            }
        }
    }
    public class MultNode : MultiplNode
    {
        public MultNode(Node _left, Node _right) : base(_left, _right) { }
        public MultNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("mul");
            return null;
        }
    }
    public class DivNode : MultiplNode
    {
        public DivNode(Node _left, Node _right) : base(_left, _right) { }
        public DivNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("div");
            return null;
        }
    }
    public class AdditiveNode : Node
    {
        public Node left;
        public Node right;

        public AdditiveNode(Node _left, Node _right)
        {
            left = _left;
            right = _right;
        }
        public AdditiveNode() { }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                left.SetRefParentCodeNode(parentCodeNode);
                right.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override string GenCode()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return null;
            left.GenCode();
            if (left.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            right.GenCode();
            if (right.CheckType() != type)
            {
                EmitCode("conv.r8");
            }
            return null;

        }
        public override int GetStackDepth()
        {
            int r = right.GetStackDepth(), l = left.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override Parser.Types CheckType()
        {
            Parser.Types left_type = left.CheckType();
            Parser.Types right_type = right.CheckType();

            if (left_type == Parser.Types.BooleanType || left_type == Parser.Types.NoneType || left_type == Parser.Types.StringType ||
                right_type == Parser.Types.BooleanType || right_type == Parser.Types.NoneType || right_type == Parser.Types.StringType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: type error");
                return Parser.Types.NoneType;
            }
            else if (left_type == Parser.Types.DoubleType || right_type == Parser.Types.DoubleType)
            {
                return Parser.Types.DoubleType;
            }
            else
            {
                return left_type;
            }
        }
    }
    public class MinusNode : AdditiveNode
    {
        public MinusNode(Node _left, Node _right) : base(_left, _right) { }
        public MinusNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("sub");
            return null;
        }
    }
    public class PlusNode : AdditiveNode
    {
        public PlusNode(Node _left, Node _right) : base(_left, _right) { }
        public PlusNode() { }
        public override string GenCode()
        {
            base.GenCode();
            EmitCode("add");
            return null;
        }

    }
    public class IfNode : Node
    {
        public Node condition;
        public Node block;
        public IfNode() { }
        public IfNode(Node _condition, Node _block)
        {
            condition = _condition;
            block = _block;
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                condition.SetRefParentCodeNode(parentCodeNode);
                block.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override int GetStackDepth()
        {
            int r = block.GetStackDepth(), l = condition.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override string GenCode()
        {
            BlockNode bl1 = block as BlockNode;
            if (bl1 is null)
            {
                Console.WriteLine($"line {linenumber} error: syntax error");
                errors++;
                return null;
            }
            if (condition.CheckType() != Parser.Types.BooleanType)
            {
                Console.WriteLine($"line {linenumber} error: bad type of condition expresion");
                errors++;
                return null;
            }
            condition.GenCode();
            EmitCode($"brfalse  {bl1.blockId}_2");
            bl1.GenCode();
            EmitCode($"{bl1.blockId}_2: nop");
            return null;
        }
        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
                block.SetRefForWhile(whileRef);
            }
        }
        public override void GenIdents()
        {
            block.GenIdents();
        }
    }
    public class IfElseNode : Node
    {
        public Node condition;
        public Node blockAfterIf;
        public Node blockAfterElse;
        public IfElseNode() { }
        public IfElseNode(Node _condition, Node _blockAfterIf, Node _blockAfterElse)
        {
            condition = _condition;
            blockAfterIf = _blockAfterIf;
            blockAfterElse = _blockAfterElse;
        }

        public override int GetStackDepth()
        {
            int r = condition.GetStackDepth(), l = blockAfterIf.GetStackDepth(), h = blockAfterElse.GetStackDepth();

            return ((r > l ? r : l) > h? (r > l ? r : l):h )+ 1 ;
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                condition.SetRefParentCodeNode(parentCodeNode);
                blockAfterElse.SetRefParentCodeNode(parentCodeNode);
                blockAfterIf.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override string GenCode()
        {
            BlockNode bl1 = blockAfterIf as BlockNode;
            BlockNode bl2 = blockAfterElse as BlockNode;
            if (bl1 is null || bl2 is null)
            {
                Console.WriteLine($"line {linenumber} error: syntax error");
                errors++;
                return null;
            }
            if (condition.CheckType() != Parser.Types.BooleanType)
            {
                Console.WriteLine($"line {linenumber} error: bad type of condition expresion");
                errors++;
                return null;
            }
            condition.GenCode();
            EmitCode($"brfalse  {bl2.blockId}");
            bl1.GenCode();
            EmitCode($"br  {bl1.blockId}_2");
            bl2.GenCode();
            EmitCode($"{bl1.blockId}_2: nop");
            return null;
        }
        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
                blockAfterIf.SetRefForWhile(whileRef);
                blockAfterElse.SetRefForWhile(whileRef);
            }
        }
        public override void GenIdents()
        {
            blockAfterIf.GenIdents();
            blockAfterElse.GenIdents();
        }
    }
    public class WhileNode : Node
    {
        public Node condition;
        public BlockNode block;
        public string continueJump;
        public string breakJump;

        public WhileNode(Node _condition, Node _block, int _line)
        {
            linenumber = _line;
            condition = _condition;
            block = _block as BlockNode;
            if (block is null)
            {
                Console.WriteLine($"line {linenumber} error: syntax error");
                errors++;
            }
            else
            {
                block.SetRefForWhile(this);
                continueJump = $"{block.blockId}_cont";
                breakJump = $"{block.blockId}_break";
            }
        }
        public override int GetStackDepth()
        {
            int r = condition.GetStackDepth(), l = block.GetStackDepth();

            return (r > l ? r : l) + 1;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                condition.SetRefParentCodeNode(parentCodeNode);
                block.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            if (condition.CheckType() != Parser.Types.BooleanType)
            {
                Console.WriteLine($"line {linenumber} error: bad type of condition expresion");
                errors++;
                return null;
            }
            condition.GenCode();
            EmitCode($"brfalse  {breakJump}");
            block.GenCode();
            EmitCode($"{continueJump}: nop");
            condition.GenCode();
            EmitCode($"brtrue  {block.blockId}");
            EmitCode($"{breakJump}: nop");
            return null;
        }
        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
            }
        }
        public override void GenIdents()
        {
            block.GenIdents();
        }
    }
    public class WriteNode : Node
    {
        public Node writable_exp;

        public WriteNode() { }
        public WriteNode(Node _writable_exp)
        {
            writable_exp = _writable_exp;
        }
        public override int GetStackDepth()
        {
            return writable_exp.GetStackDepth() + 4;
        }
        public override Parser.Types CheckType()
        {
            return writable_exp.CheckType();
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                writable_exp.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override string GenCode()
        {
            switch (writable_exp.CheckType())
            {
                case Parser.Types.BooleanType:
                    writable_exp.GenCode();
                    EmitCode("call       void [mscorlib]System.Console::Write(bool)");
                    break;
                case Parser.Types.DoubleType:
                    EmitCode("call       class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                    EmitCode("ldstr      \"{0:0.000000}\"");
                    writable_exp.GenCode();
                    EmitCode("box        [mscorlib]System.Double");
                    EmitCode("call       string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider, string, object)");
                    EmitCode("call       void [mscorlib]System.Console::Write(string)");
                    break;
                case Parser.Types.StringType:
                    writable_exp.GenCode();
                    EmitCode("call       void [mscorlib]System.Console::Write(string)");
                    break;
                case Parser.Types.IntegerType:
                    writable_exp.GenCode();
                    EmitCode("call       void [mscorlib]System.Console::Write(int32)");
                    break;
                default:
                    errors++;
                    Console.WriteLine($"line {linenumber} error: syntax error");
                    Console.WriteLine();
                    return null;
            }
            return null;
        }
    }
    public class ReadNode : Node
    {
        public IdentNode ident;

        public ReadNode() { }
        public ReadNode(Node _ident)
        {
            ident = _ident as IdentNode;
        }

        public override Parser.Types CheckType()
        {
            return ident.CheckType();
        }
        public override int GetStackDepth()
        {
            return ident.GetStackDepth() + 3;
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                ident.SetRefParentCodeNode(parentCodeNode);
            }
        }

        public override string GenCode()
        {
            if (ident is null)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: undefined");
                return null;
            }
            if (ident.CheckType() == Parser.Types.NoneType)
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: undefined identifier");
                return null;
            }

            ident.GenSetValueBefore();

            EmitCode("call  string [mscorlib]System.Console::ReadLine()");

            switch (ident.CheckType())
            {
                case Parser.Types.IntegerType:
                    {
                        EmitCode("call   int32 [mscorlib]System.Int32::Parse(string)");
                        break;
                    }
                case Parser.Types.DoubleType:
                    {
                        EmitCode("call class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()");
                        EmitCode("call       float64 [mscorlib]System.Double::Parse(string, class [mscorlib] System.IFormatProvider)");
                        break;
                    }
                case Parser.Types.BooleanType:
                    {
                        EmitCode("call  bool [mscorlib]System.Boolean::Parse(string)");
                        break;
                    }
                default:
                    {
                        errors++;
                        Console.WriteLine($"line {linenumber} error: bad variable");
                        return null;
                    }
            }
            ident.GenSetValue();
            return null;
        }
    }
    public class IdentNode : Node
    {
        public string identifier;
        public string usable_Id;
        public IdentNode() { }
        public IdentNode(string _identifier)
        {
            identifier = _identifier;
        }
        public override Parser.Types CheckType()
        {
            DecType type = new DecType();
            CodeNode node = refParentCodeNode as CodeNode;
            while (!(node is null) && !(node.idents.TryGetValue(identifier, out type)))
            {
                node = node.refParentCodeNode as CodeNode;
            }
            if (type.type != Parser.Types.NoneType)
            {
                if (type.isTable)
                {
                    Console.WriteLine($"line {linenumber} error: trying to use table like normal variable");
                    errors++;
                    return Parser.Types.NoneType;
                }
                usable_Id = $"{node.codeId}_{identifier}";
                return type.type;
            }
            else
            {
                Console.WriteLine($"line {linenumber} error: undefined identifier");
                errors++;
                return Parser.Types.NoneType;
            }
        }

        public override string GenCode()
        {
            if (this.CheckType() != Parser.Types.NoneType) ;
            EmitCode($"ldloc '{usable_Id}'");
            return null;
        }

        public virtual void GenSetValueBefore() { }
        public virtual void GenSetValue()
        {
            EmitCode($"stloc '{usable_Id}'");
        }
    }
    public class TabIdentNode : IdentNode
    {
        public List<Node> indexes;
        private bool create;
        public TabIdentNode(string ident, List<Node> _indexes, bool _create = false) : base(ident)
        {
            indexes = _indexes;
            create = _create;
        }
        public override int GetStackDepth()
        {
            int max = 0;
            foreach(var index in indexes)
            {
                int tmp = index.GetStackDepth();
                if (tmp > max) max = tmp;
            }
            return max + indexes.Count + 2;
        }
        public override Parser.Types CheckType()
        {
            DecType type = new DecType();
            CodeNode node = refParentCodeNode as CodeNode;
            while (!(node is null) && !(node.idents.TryGetValue(identifier, out type)))
            {
                node = node.refParentCodeNode as CodeNode;
            }
            if (type.type != Parser.Types.NoneType)
            {
                if (!type.isTable)
                {
                    Console.WriteLine($"line {linenumber} error: identifier is not a table");
                    errors++;
                    return Parser.Types.NoneType;
                }
                if (type.tableIndexesNum != indexes.Count)
                {
                    Console.WriteLine($"line {linenumber} error: wrong number of dimmensions");
                    errors++;
                    return Parser.Types.NoneType;
                }
                int dimm = 1;
                foreach (var expr in indexes)
                {
                    if (expr.CheckType() != Parser.Types.IntegerType)
                    {
                        Console.WriteLine($"line {linenumber} error: wrong type of {dimm} dimmension index");
                        errors++;
                        return Parser.Types.NoneType;
                    }
                    ++dimm;
                }
                usable_Id = $"{node.codeId}_{identifier}";
                return type.type;
            }
            else
            {
                Console.WriteLine($"line {linenumber} error: undefined identifier");
                errors++;
                return Parser.Types.NoneType;
            }
        }
        public override void SetRefForWhile(Node whileRef)
        {
            base.SetRefForWhile(whileRef);
            foreach(var ind in indexes)
            {
                ind.SetRefForWhile(whileRef);
            }
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            refParentCodeNode = parentCodeNode;
            foreach (var ind in indexes)
            {
                ind.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override string GenCode()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return null;
            if (create)
            {
                foreach (var index in indexes)
                {
                    index.GenCode();
                }
                if (indexes.Count > 1)
                {
                    string inside = "0...";
                    string args = "int32";
                    for (int i = 1; i < indexes.Count; i++)
                    {
                        inside += ",0...";
                        args += ",int32";
                    }
                    string stype = null;
                    switch (type)
                    {
                        case Parser.Types.BooleanType:
                            stype = "bool";
                            break;
                        case Parser.Types.DoubleType:
                            stype = "float64";
                            break;
                        case Parser.Types.IntegerType:
                            stype = "int32";
                            break;
                    }
                    EmitCode($"newobj instance void {stype}[{inside}]::.ctor({args})");
                }
                else
                {
                    string stype = null;
                    switch (type)
                    {
                        case Parser.Types.BooleanType:
                            stype = "Boolean";
                            break;
                        case Parser.Types.DoubleType:
                            stype = "Double";
                            break;
                        case Parser.Types.IntegerType:
                            stype = "Int32";
                            break;
                    }
                    EmitCode($"newarr [mscorlib]System.{stype}");
                }
                EmitCode($"stloc '{usable_Id}'");
            }
            else
            {
                EmitCode($"ldloc '{usable_Id}'");
                if (indexes.Count > 1)
                {
                    string inside = "0...";
                    string args = "int32";
                    string stype = null;
                    switch (type)
                    {
                        case Parser.Types.BooleanType:
                            stype = "bool";
                            break;
                        case Parser.Types.DoubleType:
                            stype = "float64";
                            break;
                        case Parser.Types.IntegerType:
                            stype = "int32";
                            break;
                    }
                    for (int i = 1; i < indexes.Count; i++)
                    {
                        inside += ",0...";
                        args += ",int32";
                    }
                    foreach (var index in indexes)
                    {
                        index.GenCode();
                    }
                    EmitCode($"call instance {stype} {stype}[{inside}]::Get({args})");
                }
                else
                {
                    indexes[0].GenCode();
                    switch (type)
                    {
                        case Parser.Types.BooleanType:
                            EmitCode($"ldelem.u1");
                            break;
                        case Parser.Types.DoubleType:
                            EmitCode($"ldelem.r8");
                            break;
                        case Parser.Types.IntegerType:
                            EmitCode($"ldelem.i4");
                            break;
                    }
                }

            }
            return null;
        }
        public override void GenSetValueBefore()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return;
            EmitCode($"ldloc '{usable_Id}'");
            foreach (var index in indexes)
            {
                index.GenCode();
            }
        }
        public override void GenSetValue()
        {
            var type = this.CheckType();
            if (type == Parser.Types.NoneType)
                return;
            if (indexes.Count > 1)
            {
                string inside = "0...";
                string args = "int32";
                for (int i = 1; i < indexes.Count; i++)
                {
                    inside += ",0...";
                    args += ",int32";
                }
                string stype = null;
                switch (type)
                {
                    case Parser.Types.BooleanType:
                        stype = "bool";
                        break;
                    case Parser.Types.DoubleType:
                        stype = "float64";
                        break;
                    case Parser.Types.IntegerType:
                        stype = "int32";
                        break;
                }
                EmitCode($"call instance void {stype}[{inside}]::Set({args},{stype})");
            }
            else
            {
                switch (type)
                {
                    case Parser.Types.BooleanType:
                        EmitCode("stelem.i1");
                        break;
                    case Parser.Types.DoubleType:
                        EmitCode("stelem.r8");
                        break;
                    case Parser.Types.IntegerType:
                        EmitCode("stelem.i4");
                        break;
                }
            }
        }
    }
    public class IntNumberNode : Node
    {
        public int value;

        public IntNumberNode() { }
        public IntNumberNode(string number)
        {
            if (!int.TryParse(number, out value))
            {
                Compiler.errors++;
                Console.WriteLine($" line:{base.linenumber} syntax error");
            }
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.IntegerType;
        }

        public override string GenCode()
        {
            EmitCode($"ldc.i4 {value}");
            return null;
        }
    }
    public class DoubleNumberNode : Node
    {
        public double value;

        public DoubleNumberNode() { }
        public DoubleNumberNode(string number)
        {
            if (!double.TryParse(number, NumberStyles.Any, CultureInfo.InvariantCulture, out value))
            {
                Compiler.errors++;
                Console.WriteLine($" line:{base.linenumber} syntax error");
            }
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.DoubleType;
        }

        public override string GenCode()
        {
            EmitCode($"ldc.r8 {value.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            return null;
        }
    }
    public class BooleanNode : Node
    {
        public bool value;

        public BooleanNode() { }
        public BooleanNode(string number)
        {
            if (!bool.TryParse(number, out value))
            {
                Compiler.errors++;
                Console.WriteLine($" line:{base.linenumber} syntax error");
            }
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.BooleanType;
        }

        public override string GenCode()
        {
            if (value)
                EmitCode("ldc.i4.1");
            else
                EmitCode("ldc.i4.0");
            return null;
        }
    }
    public class AssignNode : Node
    {
        public Node expr;
        public IdentNode ident;

        public AssignNode() { }
        public AssignNode(IdentNode i, Node e)
        {
            expr = e;
            ident = i;
        }

        public override int GetStackDepth()
        {
            int r = ident.GetStackDepth(), l = expr.GetStackDepth();

            return r + l + 3;
        }

        public override Parser.Types CheckType()
        {
            return ident.CheckType();
        }

        public override string GenCode()
        {
            var id_type = ident.CheckType();
            var ex_type = expr.CheckType();
            if (id_type != ex_type &&
                !(id_type == Parser.Types.DoubleType &&
                  ex_type == Parser.Types.IntegerType))
            {
                errors++;
                Console.WriteLine($"line {linenumber} error: bad type");
            }
            else
            {
                ident.GenSetValueBefore();
                expr.GenCode();
                if (id_type == Parser.Types.DoubleType && ex_type != id_type)
                    EmitCode("conv.r8");
                EmitCode("dup");
                switch(id_type)
                {
                    case Parser.Types.BooleanType:
                        EmitCode("stloc 'tempb'");
                        break;
                    case Parser.Types.DoubleType:
                        EmitCode("stloc 'tempf'");
                        break;
                    case Parser.Types.IntegerType:
                        EmitCode("stloc 'tempi'");
                        break;
                }
                ident.GenSetValue();
                switch (id_type)
                {
                    case Parser.Types.BooleanType:
                        EmitCode("ldloc 'tempb'");
                        break;
                    case Parser.Types.DoubleType:
                        EmitCode("ldloc 'tempf'");
                        break;
                    case Parser.Types.IntegerType:
                        EmitCode("ldloc 'tempi'");
                        break;
                }

            }
            return null;
        }

        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                ident.SetRefParentCodeNode(parentCodeNode);
                expr.SetRefParentCodeNode(parentCodeNode);
            }
        }
    }
    public class StringNode : Node
    {
        public string st;

        public StringNode() { }
        public StringNode(string _st)
        {
            st = _st;
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.StringType;
        }

        public override string GenCode()
        {
            EmitCode($"ldstr {st}");
            return null;
        }
    }
    public class ReturnNode : Node
    {
        public ReturnNode() { }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            EmitCode("leave EndMain");
            return null;
        }
    }
    public class BlockNode : Node
    {
        public string blockId;
        public Node inside;
        public BlockNode() { blockId = "stb_" + Compiler.blockIdCounter++; }

        public BlockNode(Node _inside) { inside = _inside; blockId = "stb" + Compiler.blockIdCounter++; }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }
        public override int GetStackDepth()
        {
            return inside.GetStackDepth();
        }
        public override string GenCode()
        {
            EmitCode($"{blockId} : nop");
            inside.GenCode();
            return null;
        }

        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
                inside.SetRefForWhile(whileRef);
            }
        }
        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                inside.SetRefParentCodeNode(parentCodeNode);
            }
        }
        public override void GenIdents()
        {
            inside.GenIdents();
        }
    }
    public class CodeNode : Node
    {
        public List<Node> inside;
        public Dictionary<string, DecType> idents;
        public string codeId;
        public CodeNode()
        {
            inside = new List<Node>();
            codeId = $"cn_{codeIdCounter++}";
            idents = new Dictionary<string, DecType>();
        }
        public CodeNode(List<Node> _inside)
        {
            inside = _inside;
            codeId = $"cn_{codeIdCounter++}";
            idents = new Dictionary<string, DecType>();
        }
        public override int GetStackDepth()
        {
            int max = 0;
            foreach (var index in inside)
            {
                int tmp = index.GetStackDepth();
                if (tmp > max) max = tmp;
            }
            return max;
        }
        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            foreach (var instr in inside)
            {
                instr.GenCode();
            }
            return null;
        }

        public override void SetRefParentCodeNode(Node parentCodeNode)
        {
            if (refParentCodeNode is null)
            {
                refParentCodeNode = parentCodeNode;
                foreach (var instr in inside)
                {
                    instr.SetRefParentCodeNode(this);
                }
            }
        }

        public override void GenIdents()
        {
            foreach (var variable in idents)
            {
                string type = null;
                switch (variable.Value.type)
                {
                    case Parser.Types.BooleanType:
                        type = "bool";
                        break;
                    case Parser.Types.DoubleType:
                        type = "float64";
                        break;
                    case Parser.Types.IntegerType:
                        type = "int32";
                        break;
                    default:
                        errors++;
                        Console.WriteLine($"error: bad type");
                        break;
                }
                if (type is null) continue;
                if (variable.Value.isTable)
                {
                    if (variable.Value.tableIndexesNum > 1)
                    {
                        string inside = "0...";
                        for (int i = 1; i < variable.Value.tableIndexesNum; i++)
                        {
                            inside += ",0...";
                        }
                        EmitCode($".locals init ( {type}[{inside}] '{codeId}_{variable.Key}' )");
                    }
                    else
                    {
                        EmitCode($".locals init ( {type}[] '{codeId}_{variable.Key}' )");
                    }
                }
                else
                    EmitCode($".locals init ( {type} '{codeId}_{variable.Key}' )");
            }
            foreach (var node in inside)
            {
                node.GenIdents();
            }
            EmitCode();
            if (codeId == "super")
            {
                foreach (var variable in idents)
                {
                    if (variable.Value.isTable) continue;
                    switch (variable.Value.type)
                    {
                        case Parser.Types.BooleanType:
                            EmitCode($"ldc.i4.0");
                            break;
                        case Parser.Types.DoubleType:
                            EmitCode($"ldc.r8 0.0");
                            break;
                        case Parser.Types.IntegerType:
                            EmitCode($"ldc.i4.0");
                            break;
                        default:
                            errors++;
                            Console.WriteLine($"error: bad type");
                            break;
                    }
                    EmitCode($"stloc  '{codeId}_{variable.Key}'");
                }
            }
        }

        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
                foreach (var node in inside)
                {
                    node.SetRefForWhile(whileRef);
                }
            }
        }
    }
    public class ContinueNode : Node
    {
        public ContinueNode() { }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            WhileNode node = refForWhile as WhileNode;
            if (node is null)
            {
                Console.WriteLine($"line {linenumber} error: continue not inside any while");
                errors++;
                return null;
            }
            EmitCode($"br {node.continueJump}");
            return null;
        }

        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
            }
        }
    }
    public class BreakNode : Node
    {
        public int numOfJumps;
        public BreakNode(int _linenumber) { numOfJumps = 1; linenumber = _linenumber; }
        public BreakNode(Node num, int _linenumber)
        {
            linenumber = _linenumber;
            IntNumberNode node = num as IntNumberNode;
            if (node is null)
            {
                Console.WriteLine($"line {linenumber} error: syntax error");
                errors++;
            }
            if (node.value < 1)
            {
                Console.WriteLine($"line {linenumber} error: syntax error");
                errors++;
            }
            numOfJumps = node.value;
        }

        public override Parser.Types CheckType()
        {
            return Parser.Types.NoneType;
        }

        public override string GenCode()
        {
            WhileNode node = refForWhile as WhileNode;
            if (node is null)
            {
                Console.WriteLine($"line {linenumber} error: break not inside any whiles");
                errors++;
                return null;
            }
            for (int i = 1; i < numOfJumps; i++)
            {
                node = node.refForWhile as WhileNode;
                if (node is null)
                {
                    Console.WriteLine($"line {linenumber} error: continue inside only {i} whiles ");
                    errors++;
                    return null;
                }
            }
            EmitCode($"br {node.breakJump}");
            return null;
        }

        public override void SetRefForWhile(Node whileRef)
        {
            if (refForWhile is null)
            {
                refForWhile = whileRef;
            }
        }
    }

    public static void EmitCode(string instr = null)
    {
        sw.WriteLine(instr);
    }

    public static void EmitCode(string instr, params object[] args)
    {
        sw.WriteLine(instr, args);
    }

    private static StreamWriter sw;

    private static void GenProlog()
    {
        EmitCode(".assembly extern mscorlib { }");
        EmitCode(".assembly calculator { }");
        EmitCode(".method static void main()");
        EmitCode("{");
        EmitCode(".entrypoint");
        EmitCode(".try");
        EmitCode("{");
        EmitCode();
        EmitCode("// prolog");
        EmitCode();
        if(!(treeRoot is null))
        maxStack = treeRoot.GetStackDepth();
        if(maxStack > 8)
        EmitCode($".maxstack {maxStack}");
        EmitCode($".locals init ( float64 'tempf' )");
        EmitCode($".locals init ( int32 'tempi' )");
        EmitCode($".locals init ( bool 'tempb' )");
    }

    private static void GenEpilog()
    {
        EmitCode("leave EndMain");
        EmitCode("}");
        EmitCode("catch [mscorlib]System.Exception");
        EmitCode("{");
        EmitCode("callvirt instance string [mscorlib]System.Exception::get_Message()");
        EmitCode("call void [mscorlib]System.Console::WriteLine(string)");
        EmitCode("leave EndMain");
        EmitCode("}");
        EmitCode("EndMain: ret");
        EmitCode("}");
    }

}
