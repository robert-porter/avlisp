using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVLisp
{
    class Env
    {
        public Env(Env parentEnv = null)
        {
            ParentEnv = parentEnv;
            Variables = new Dictionary<String, LispNode>();
        }

        public void Add(String varName, LispNode lispNode)
        {
            Variables.Add(varName, lispNode);
        }

        
        public void Set(String varName, LispNode lispNode)
        {
            Variables[varName] = lispNode;
        }

        public LispNode Find(String varName)
        {
            LispNode lispNode;
            if (Variables.TryGetValue(varName, out lispNode))
                return lispNode;
            else if (ParentEnv == null)
                return null; // undefined variable.
            else
                return ParentEnv.Find(varName);
        }

        Env ParentEnv;
        Dictionary<String, LispNode> Variables;
    }

    abstract class LispNode
    {
        public abstract LispNode Evaluate(Env env);
    }

    class IntNode : LispNode
    {
        public IntNode(int v) { Value = v; }
        public int Value;

        public override LispNode Evaluate(Env env) { return this; }
    }

    class BoolNode : LispNode
    {
        public BoolNode(bool b) { Value = b;  }
        public bool Value;

        public override LispNode Evaluate(Env env) { return this; }
    }

    class QuoteNode : LispNode
    {
        public QuoteNode(String s) { Value = s; }
        public String Value;
        public override LispNode Evaluate(Env env)
        {
            return this;
        }
    }

    class DefineNode : LispNode
    {
        String Name;
        LispNode Expression;

        public DefineNode(String name, LispNode expression)
        {
            Name = name;
            Expression = expression;
        }

        public override LispNode Evaluate(Env env)
        {
            env.Add(Name, Expression.Evaluate(env));
            return this; // return this???
        }
    }

    class VarNode : LispNode
    {
        public String Name;

        public VarNode(String name)
        {
            Name = name;
        }

        public override LispNode Evaluate(Env env)
        {
            return env.Find(Name);
        }
    }

    class BeginNode : LispNode
    {
        public List<LispNode> Expressions;

        public BeginNode()
        {
            Expressions = new List<LispNode>();
        }

        public override LispNode Evaluate(Env env)
        {
            for(int i = 0; i < Expressions.Count - 1; i++)
            {
                Expressions[i].Evaluate(env);
            }
            return Expressions[Expressions.Count - 1].Evaluate(env);
        }
       
    }

    class SetNode : LispNode
    {
        public String VariableName;
        public LispNode Expression;

        public SetNode()
        {
           
        }

        public override LispNode Evaluate(Env env)
        {
            if (env.Find(VariableName) == null)
                return null;

            LispNode exp = Expression.Evaluate(env);
            env.Set(VariableName, exp);
            return exp; // probably not ideomatic scheme but we can chain set!s.
        }
    }

    class IfNode : LispNode
    {
        public LispNode a, b, c; // if a then b else c

        public override LispNode Evaluate(Env env)
        {
            object test = a.Evaluate(env);
            if (!(test is BoolNode))
                return null;
            else if ((test as BoolNode).Value)
                return b.Evaluate(env);
            else
                return c.Evaluate(env);
        }
    }
    class LambdaNode : LispNode
    {
        public List<String> Arguments;
        public LispNode Body;
        public Env Env;

        public LambdaNode()
        {
            Arguments = new List<String>();
            Env = null;
        }

        public override LispNode Evaluate(Env env)
        {
            Env = env;
            return this;
        }

        public virtual LispNode Call(Env env)
        {
            return Body.Evaluate(env);
        }
    }

    class CallNode : LispNode
    {
        public LispNode What;
        public List<LispNode> Arguments;

        public CallNode()
        {
            Arguments = new List<LispNode>();
        }

        public override LispNode Evaluate(Env env)
        {
            LispNode func = null;
            LambdaNode lambdaNode = null;
            
            func = What.Evaluate(env);

            if (!(func is LambdaNode))
                return null;

            lambdaNode = func as LambdaNode;

            if (lambdaNode.Arguments.Count != Arguments.Count)
                return null;

            Env newEnv = new Env(lambdaNode.Env); // use the lambda env, not the global env, for lexical closures

            for (int i = 0; i < Arguments.Count; i++)
            {
                newEnv.Add(lambdaNode.Arguments[i], Arguments[i].Evaluate(env));
            }

            return lambdaNode.Call(newEnv);
        }
    }

  
    class ArithmeticNode : LambdaNode
    {
        Func<int, int, int> Function;

        public ArithmeticNode(Func<int, int, int> f)
        {
            Function = f;
            Arguments.Add("__argument1");
            Arguments.Add("__argument2");
        }

        public override LispNode Call(Env env)
        {
            LispNode lispArg1 = env.Find("__argument1");
            LispNode lispArg2 = env.Find("__argument2");

            if (!(lispArg1 is IntNode || lispArg2 is IntNode))
                return null;
            int arg1 = (lispArg1 as IntNode).Value;
            int arg2 = (lispArg2 as IntNode).Value;

            return new IntNode(Function(arg1, arg2));
        }
    }

    class ComparisonNode : LambdaNode
    {
        Func<int, int, bool> Function;

        public ComparisonNode(Func<int, int, bool> f)
        {
            Function = f;
            Arguments.Add("__argument1");
            Arguments.Add("__argument2");
        }

        public override LispNode Call(Env env)
        {
            LispNode lispArg1 = env.Find("__argument1");
            LispNode lispArg2 = env.Find("__argument2");

            if (!(lispArg1 is IntNode || lispArg2 is IntNode))
                return null;
            int arg1 = (lispArg1 as IntNode).Value;
            int arg2 = (lispArg2 as IntNode).Value;

            return new BoolNode(Function(arg1, arg2));
        }
    }

    class BooleanOpNode : LambdaNode
    {
        Func<bool, bool, bool> Function;

        public BooleanOpNode(Func<bool, bool, bool> f)
        {
            Function = f;
            Arguments.Add("__argument1");
            Arguments.Add("__argument2");
        }

        public override LispNode Call(Env env)
        {
            LispNode lispArg1 = env.Find("__argument1");
            LispNode lispArg2 = env.Find("__argument2");

            if (!(lispArg1 is BoolNode || lispArg2 is BoolNode))
                return null;
            bool arg1 = (lispArg1 as BoolNode).Value;
            bool arg2 = (lispArg2 as BoolNode).Value;

            return new BoolNode(Function(arg1, arg2));
        }
    }

    class NotNode : LambdaNode
    {
        Func<bool, bool> Function;

        public NotNode(Func< bool, bool> f)
        {
            Function = f;
            Arguments.Add("__argument1");
        }

        public override LispNode Call(Env env)
        {
            LispNode lispArg1 = env.Find("__argument1");

            if (!(lispArg1 is BoolNode))
                return null;
            bool arg1 = (lispArg1 as BoolNode).Value;

            return new BoolNode(Function(arg1));
        }
    }


    class Interpreter
    {
        public object Execute(String exp)
        {
            LispNode program = Read(TokenizeStream(exp));
            Env env = CreateGlobals();
            LispNode result = program.Evaluate(env);

            if (result is IntNode)
            {
                return (result as IntNode).Value;
            }
            else if (result is BoolNode)
            {
                return (result as BoolNode).Value;
            }
            else if (result is QuoteNode)
            {
                return (result as QuoteNode).Value;
            }
            else
                return null;
        }

        Env CreateGlobals()
        {
            Env env = new Env();

            env.Add("+", new ArithmeticNode(delegate(int x, int y) { return x + y; }));
            env.Add("-", new ArithmeticNode(delegate(int x, int y) { return x - y; }));
            env.Add("*", new ArithmeticNode(delegate(int x, int y) { return x * y; }));
            env.Add("/", new ArithmeticNode(delegate(int x, int y) { return x / y; }));
            env.Add("<", new ComparisonNode(delegate(int x, int y) { return x < y; }));
            env.Add("<=", new ComparisonNode(delegate(int x, int y) { return x <= y; }));
            env.Add(">", new ComparisonNode(delegate(int x, int y) { return x > y; }));
            env.Add(">=", new ComparisonNode(delegate(int x, int y) { return x >= y; }));
            env.Add("=", new ComparisonNode(delegate(int x, int y) { return x == y; }));
            env.Add("and", new BooleanOpNode(delegate(bool x, bool y) { return x && y; }));
            env.Add("or", new BooleanOpNode(delegate(bool x, bool y) { return x || y; }));
            env.Add("not", new BooleanOpNode(delegate(bool x, bool y) { return x || y; }));

            return env;
        }
        
        public static List<String> TokenizeStream(String stream)
        {

            return stream
                .Replace("(", " ( ")
                .Replace(")", " ) ")
                .Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                .ToList();

        }

        static LispNode Read(List<String> tokens)
        {
            if (tokens.Count() == 0) return null;

            String token = tokens[0];
            tokens.RemoveAt(0);

            if (token == "(")
            {
                token = tokens[0];

                
                if(token == "if")
                {
                    tokens.RemoveAt(0);
                    IfNode ifNode = new IfNode();
                    ifNode.a = Read(tokens);
                    ifNode.b = Read(tokens);
                    ifNode.c = Read(tokens);
                    if (tokens[0] != ")")
                        return null;
                    else
                    {
                        tokens.RemoveAt(0); // eat close paren
                        return ifNode;
                    }
                }
                else if (token == "quote")
                {
                    tokens.RemoveAt(0);
                    StringBuilder builder = new StringBuilder();
                    while (tokens[0] != ")")
                    {
                        builder.Append(tokens[0]);
                        tokens.RemoveAt(0);
                    }
                    QuoteNode quoteNode = new QuoteNode(builder.ToString());
                    tokens.RemoveAt(0);
                    return quoteNode;                    
                }
                else if(token == "define")
                {
                    tokens.RemoveAt(0);
                    String name = tokens[0];
                    tokens.RemoveAt(0);
                    LispNode expression = Read(tokens);
                    DefineNode defNode = new DefineNode(name, expression);
                    if (tokens[0] != ")")
                        return null;
                    else
                    {
                        tokens.RemoveAt(0); // eat close paren
                        return defNode;
                    }
                    
                }
                else if(token == "begin")
                {
                    tokens.RemoveAt(0);
                    BeginNode beginNode = new BeginNode();

                    do
                    {
                        beginNode.Expressions.Add(Read(tokens));
                    } while (tokens[0] != ")");
                    tokens.RemoveAt(0); // eat close paren
                    return beginNode;
                }
                else if (token == "lambda")
                {
                    tokens.RemoveAt(0);
                    LambdaNode lambdaNode = new LambdaNode();
                    if (tokens[0] != "(")
                        return null;
                    tokens.RemoveAt(0);
                    while(tokens[0] != ")")
                    {
                        lambdaNode.Arguments.Add(tokens[0]);
                        tokens.RemoveAt(0);
                    }
                    tokens.RemoveAt(0);

                    lambdaNode.Body = Read(tokens);

                    tokens.RemoveAt(0);

                    return lambdaNode;
                }
                else if (token == "set!")
                {
                    tokens.RemoveAt(0);
                    SetNode setNode = new SetNode();
                    setNode.VariableName = tokens[0];
                    tokens.RemoveAt(0);

                    setNode.Expression = Read(tokens);

                    return setNode;
                }
                else //if(token == "call")
                {
                    CallNode callNode = new CallNode();

                    callNode.What = Read(tokens);

                    while (tokens[0] != ")")
                    {
                        callNode.Arguments.Add(Read(tokens));
                    }
                    tokens.RemoveAt(0);

                    return callNode;
                }
                //return null;
            }
            else if (token == ")")
            {
                return null;
            }
            else
                return ParseAtom(token);
        }

        public static LispNode ParseAtom(String token)
        {
            int i;
            if(int.TryParse(token, out i))
            {
                return new IntNode(i);
            }
            else if(token == "#f")
            {
                return new BoolNode(false);
            }
            else if(token == "#t")
            {
                return new BoolNode(true);
            }
            else
            {
                return new VarNode(token);
            }
        }
                

    }
}
