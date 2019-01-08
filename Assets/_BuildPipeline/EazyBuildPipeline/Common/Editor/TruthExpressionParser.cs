using System;
using System.Collections;
using System.Collections.Generic;

namespace EazyBuildPipeline
{
    public static class TruthExpressionParser
    {
        public static readonly char[] opset =
            { '!', '&', '|', '(', ')', '#' };
        static readonly char[,] priority = {
     /* ! */{ '<', '>', '>', '<', '>', '>' },
     /* & */{ '<', '>', '>', '<', '>', '>' },
     /* | */{ '<', '<', '>', '<', '>', '>' },
     /* ( */{ '<', '<', '<', '<', '=', 'E' },
     /* ) */{ '>', '>', '>', '>', '>', '>' },
     /* # */{ '<', '<', '<', '<', 'E', '=' } };

        static char CompareOperator(char op1, char op2)
        {
            return priority[Array.IndexOf(opset, op1), Array.IndexOf(opset, op2)];
        }

        static Stack<bool> OperandStack = new Stack<bool>();
        static Stack<char> OperatorStack = new Stack<char>();
        public static bool Parse(string expression, bool[] values)
        {
            OperandStack.Clear(); //Hack:在确保运算正确的情况下可以不用Clear
            OperatorStack.Clear();
            OperatorStack.Push('#');
            int operandIndex = 0;
            char c;
            for (int i = 0; i < expression.Length;)
            {
                c = expression[i];
                if (c == 'o') //操作数直接入栈 (该字母表示占位一个操作数)
                {
                    OperandStack.Push(values[operandIndex++]);
                    i++;
                }
                else //操作符
                {
                    switch (CompareOperator(OperatorStack.Peek(), c))
                    {
                        case '<': //栈顶操作符优先级低，当前操作符入栈
                            OperatorStack.Push(c);
                            i++;
                            break;
                        case '=': //脱括号或脱#
                            OperatorStack.Pop();
                            i++;
                            break;
                        case '>': //退操作符栈，计算结果，结果入操作数栈
                            switch (OperatorStack.Pop())
                            {
                                case '!':
                                    OperandStack.Push(!OperandStack.Pop());
                                    break;
                                case '|':
                                    OperandStack.Push(OperandStack.Pop() | OperandStack.Pop());
                                    break;
                                case '&':
                                    OperandStack.Push(OperandStack.Pop() & OperandStack.Pop());
                                    break;
                                default:
                                    throw new EBPException("未知操作符！");
                            }
                            break;
                        case 'E':
                            throw new EBPException("括号缺失！");
                        default:
                            break;
                    }
                }
            }
            return OperandStack.Pop();
        }

        public static bool StackEmpty()
        {
            return OperandStack.Count == 0 && OperatorStack.Count == 0;
        }
    }
}
