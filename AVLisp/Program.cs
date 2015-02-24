using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AVLisp
{
    class Program
    {
        static void Main(string[] args)
        {
            Interpreter interpreter = new Interpreter();

            String exp = @"(begin (define fact (lambda (n)
                                (if  (= n 0)
                                    1
                                    (* n (fact (- n 1))))))
                             (fact 10))";

            object x = interpreter.Execute(exp);

            System.Console.Write("{0}", x);
            System.Console.ReadKey();
        }
    }
}
