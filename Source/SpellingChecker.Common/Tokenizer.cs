using System;
using System.Collections.Generic;

namespace SpellingChecker.Common
{
    public class Tokenizer
    {
    	public enum TokenType
    	{
    		Space,
    		Pontuaction,
    		Word
    	}
    	
    	public class Token
    	{
    		public TokenType Type;
        	public string Text = "";
        	public int Position;  		
    	}
    	
        private string _text;
        private int _position;
        private List<Token> _tokens = new List<Token>();

        /// <summary>
        /// Transverse text and separate elements by tokens.
        /// </summary>
        /// <returns></returns>
        public List<Token> Tokenize(string text)
        {
            this._tokens.Clear();
            this._text = text;

            while (this._position < this._text.Length)
            {
                if (!this.ParseSpace())
                {
                    if (!this.ParseWord())
                    {
                        if (!this.ParsePontuaction())
                        {
                            this._position++;
                        }
                    }
                }
            }

            return this._tokens;
        }

        /// <summary>
        /// Look for next char.
        /// </summary>
        /// <returns></returns>
        private char GetNextChar()
        {
        	char ch = '\0';
        	if (this._position < this._tokens.Count - 1)
        	{
        		ch = this._text[this._position + 1];
        	}
        	return ch;
        }

        /// <summary>
        /// Look for spaces, tabs or returns and ignore them.
        /// </summary>
        /// <returns></returns>
        private bool ParseSpace()
        {
            bool isSpace = false;

            bool keepGoing = true;
            do
            {
                switch (this._text[this._position])
                {
                    case ' ':
                    case '\t':
                    case '\r':
                    case '\n':
                        this._position++;
                        isSpace = true;
                        break;
                    default:
                        keepGoing = false;
                        break;
                }
            } while (keepGoing && this._position < this._text.Length);

            return isSpace;
        }

        /// <summary>
        /// Look for single and composed words to tokenize them.
        /// </summary>
        /// <returns></returns>
        private bool ParseWord()
        {
            bool isWord = false;
            string word = "";

            bool keepGoing = true;
            do
            {
                if (this._text[this._position] >= 'A' && this._text[this._position] <= 'z')
                {
                    word += this._text[this._position];
                    this._position++;
                    isWord = true;
                }
                else if (this._text[this._position] == '-' && isWord)
                {
                    word += this._text[this._position];
                    this._position++;
                }
                else
                {
                    keepGoing = false;
                }
            } while (keepGoing && this._position < this._text.Length);

            if (word != "")
            {
            	var newToken = new Token();
		        newToken.Type = TokenType.Word;
            	newToken.Text = word;
            	newToken.Position = this._position - word.Length;
                this._tokens.Add(newToken);
            }

            return isWord;
        }

        /// <summary>
        /// Look for dots, colon, etc, to tokenize them.
        /// </summary>
        /// <returns></returns>
        private bool ParsePontuaction()
        {
            bool isPontuaction = false;

            bool keepGoing = true;
            do
            {
                switch (this._text[this._position])
                {
                    case '.':
                    case '!':
                    case '?':
                    case ';':
                    case ',':
                		// Handles repetead pontuaction like '...', '!!!', etc..
                		char ch = this._text[this._position];
                		string tokenText = Convert.ToString(ch);
            			while (GetNextChar() == ch)
            			{
            				tokenText += ch;
            				this._position++;
            			}
                		
   		            	var newToken = new Token();
		            	newToken.Type = TokenType.Pontuaction;
   		            	newToken.Text = tokenText;
		            	newToken.Position = this._position;
		                this._tokens.Add(newToken);
                        
                        this._position++;
                        isPontuaction = true;
                        break;
                    default:
                        keepGoing = false;
                        break;
                }
            } while (keepGoing && this._position < this._text.Length);

            return isPontuaction;
        }
    }
}
