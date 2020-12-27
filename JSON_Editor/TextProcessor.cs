using System.Collections.Generic;

namespace JSON_Editor
{
    struct ValidationResult
    {
        //line number where file is not valid
        public int Row;

        //symbol poistion where file is not valid
        public int At;

        public bool IsValid;
    }

    struct LinesNumbersResult
    {
        public ulong Lines;
        public ulong Words;
    }

    static class TextProcessor
    {
        public static ValidationResult isValidJson(string text)
        {
            Stack<char> stack = new Stack<char>();
            
            ValidationResult result;
            //set initial values
            result.Row = 1; result.At = 1; result.IsValid = false;

            char last = '\0';

            foreach (char ch in text)
            {
//                char ch = ch.toLatin1();

                // ignore symbols in double quotes
                if (stack.Count != 0
                        && '"' == stack.Peek()
                        && '"' != ch
                        )
                {
                    ++result.At;
                    continue;
                }

                switch (ch)
                {
                    case '\"':
                        if (stack.Count == 0)
                        {
                            return result;
                        }
                        last = stack.Peek();
                        if (last == '\"')
                        {
                            // It's a closing quote
                            stack.Pop();
                        }
                        else
                        {
                            // It's an opening quote
                            stack.Push(ch);
                        }
                        break;
                    case ':':
                        if (stack.Count == 0)
                        {
                            return result;
                        }
                        last = stack.Peek();
                        if ('"' != last)
                        {
                            stack.Push(ch);
                        }
                        break;
                    case ',':
                        if (stack.Count == 0)
                        {
                            return result;
                        }
                        last = stack.Peek();
                        if (':' == last)
                        {
                            stack.Pop();
                        }
                        else
                        {
                            stack.Push(ch);
                        }
                        break;
                    case '{':
                        stack.Push(ch);
                        break;
                    case '}':
                        if (stack.Count == 0)
                        {
                            return result;
                        }
                        last = stack.Peek();
                        if (last == '{')
                        {
                            stack.Pop();
                        }
                        else
                        {
                            if (last == ':')
                            {
                                stack.Pop();
                                last = stack.Peek();
                                if ('{' == last)
                                {
                                    stack.Pop();
                                }
                                else
                                {
                                    return result;
                                }
                            }
                            else
                            {
                                return result;
                            }
                        }
                        break;
                    case '[':
                        stack.Push(ch);
                        break;
                    case ']':
                        if (stack.Count == 0)
                        {
                            return result;
                        }
                        last = stack.Peek();
                        while (',' == last)
                        {
                            stack.Pop();
                            last = stack.Peek();
                        }
                        if (last == '[')
                        {
                            stack.Pop();
                        }
                        else
                        {
                            return result;
                        }
                        break;
                    case '\n':
                        ++result.Row;
                        result.At = 1;
                        break;
                }
                ++result.At;
            }
            result.IsValid = (0 == stack.Count);
            return result;
        }

        public static LinesNumbersResult CountLinesWords(string text)
        {
            char[] separators = { ' ', ',', '.', '!', '?', ':',
                                    ';', ')', '(', '[', ']', '\n'};

            LinesNumbersResult result = new LinesNumbersResult();
            result.Lines = 0;  result.Words = 0;
            bool wasChar = false;

            foreach (char c in text) {
                if (System.Array.IndexOf(separators, c) == -1)
                {
                    wasChar = true;
                }
                else
                {
                    if ('\n' == c)
                    {
                        ++result.Lines;
                    }

                    if (wasChar)
                    {
                        ++result.Words;
                        wasChar = false;
                    }
                }
            }
            if (wasChar)
            {
                ++result.Words;
            }
            ++result.Lines;
            return result;
        }
    }

}
