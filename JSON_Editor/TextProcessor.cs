using System;
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

    struct TextStatistics
    {
        public ulong Lines;
        public ulong Words;
        public ulong Keys;
        public ulong Values;
    }

    static class TextProcessor
    {
        private static bool isSomethingLikeKey(ref int index, string text)
        {
            if (text.Length - 2 < index)
            {
                return false;
            }
            //check if there is anytihing 
            // anything like key
            char ch = '\0';
            do
            {
                ++index;
                ch = text[index];
                if (ch == ' '
                    || ch == '\r'
                    || ch == '\n')
                {
                    continue;
                }
                else
                if (ch == '"')
                {
                    --index;
                    return true;
                }
                else
                {
                    return false;
                }
            } while (index < text.Length - 2);

            return false;
        }

        public static ValidationResult isValidJson(string text)
        {
            Stack<char> stack = new Stack<char>();
            
            ValidationResult result;
            //set initial values
            result.Row = 1; result.At = 1; result.IsValid = false;

            char last = '\0';

            for (int i = 0; i < text.Length; ++i)
            {
                char ch = text[i];

                // ignore symbols in double quotes
                if (stack.Count != 0
                        && '"' == stack.Peek()
                        && '"' != ch
                        )
                {
                    ++result.At;
                    continue;
                }  else if (Char.IsLetter(ch)) {
                    //keywords can be without quotes
                    if (text.Substring(i, 4) == "null"
                        || text.Substring(i, 4) == "true")
                    {
                        i += 3;
                        continue;
                    }
                    else if (text.Substring(i, 5) == "false")
                    {
                        i += 4;
                        continue;
                    }
                    else
                    {
                        return result;
                    }
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

                            //check if there is anytihing 
                            // anything like value
                            do
                            {
                                ++i;
                                ch = text[i];
                                if (ch == ' ')
                                {
                                    continue;
                                } else 
                                if (ch == 'n'
                                    || ch == 't'
                                    || ch == 'f'
                                    || ch == '"'
                                    || ch == '{'
                                    || ch == '['
                                    || Char.IsDigit(ch))
                                {
                                    break;
                                } else
                                {
                                    return result;
                                }
                            } while (ch == ' ');
                            --i;
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
                            
                            if (!isSomethingLikeKey(ref i, text))
                            {
                                return result;
                            }
                        } else
                        {
                            stack.Push(ch);
                        }

                        break;
                    case '{':
                        if (isSomethingLikeKey(ref i, text))
                        {
                            stack.Push(ch);
                        } else
                        {
                            return result;
                        }
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

        // returns position of non-white space symbol
        private static int omitWhiteSpaces(int startPosition, string text)
        {
            int j = startPosition;
            bool condition;
            do
            {
                ++j;

                condition = false;
                try
                {
                    condition = ' ' == text[j] || '\r' == text[j] || '\n' == text[j];
                } catch (IndexOutOfRangeException ex)
                {
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("Everything is ok! " +
                        "Just something went wrong while calculating statistics.");
                    j = text.Length - 1;
                }

            } while ( condition && j < text.Length - 2);

            return j;
        }

        public static TextStatistics CountTextStatistics(string text)
        {
            char[] separators = { ' ', ',', '.', '!', '?', ':', '{', '}',
                                    ';', ')', '(', '[', ']', '\n', '\r', '"'};

            TextStatistics result = new TextStatistics();
            result.Lines = 0;  result.Words = 0;
            result.Keys = 0; result.Values = 0;
            bool wasChar = false;

            for (int i = 0; i < text.Length; ++i) {
                char c = text[i];
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
                    else if ('{' == c || ',' == c)
                    {
                        int temp = i;
                        if (isSomethingLikeKey(ref temp, text))
                        {
                            temp += 2;
                            int endOfKeyPosition = text.IndexOf('"', temp);
                            if (endOfKeyPosition != -1)
                            {
                                int j = omitWhiteSpaces(endOfKeyPosition, text);
                                if (':' == text[j])
                                {
                                    ++result.Keys;
                                }
                            }
                        } 
                    }
                    else if (':' == c)
                    {
                        int j = i;
                        j = omitWhiteSpaces(j, text);
                        c = text[j];

                        if (('"' == c && text.IndexOf('"', j + 1) != -1)
                            || ('{' == c && text.IndexOf('}', j + 1) != -1)
                            || ('[' == c && text.IndexOf(']', j + 1) != -1))
                        {
                            ++result.Values;
                        }
                        else if (Char.IsDigit(c))
                        {
                            double dTemp;
                            if (Double.TryParse(
                                    text.Substring(j, text.IndexOf(' ', j) - j),
                                     out dTemp))
                            {
                                ++result.Values;
                            }
                        }
                        else
                        {
                            bool condition = false;
                            try
                            {
                                condition = text.Substring(j, 4) == "null"
                                    || text.Substring(j, 4) == "true"
                                    || text.Substring(j, 5) == "false";
                            } catch (ArgumentOutOfRangeException ex)
                            {
                                Console.WriteLine(
                                    "Everything is okay! Just there is no value after :"
                                    );
                                Console.WriteLine(ex.Message);
                            }
                            if (condition)
                            {
                                ++result.Values;
                            }
                            
                        }
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
