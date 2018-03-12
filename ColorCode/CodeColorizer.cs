// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.IO;
using System.Text;
using ColorCode.Common;
using ColorCode.Compilation;
using ColorCode.Formatting;
using ColorCode.Parsing;

namespace ColorCode
{
    /// <summary>
    /// Colorizes source code.
    /// </summary>
    public class CodeColorizer : ICodeColorizer
    {
        private readonly ILanguageParser languageParser;

        /// <summary>
        /// 12/03/2018 - L.P
        /// This is necessary because otherwise there is a race
        /// when compiled languages are added to Languages.CompiledLanguages,
        /// that result in either a NullReferenceException or a duplicated item added to the 
        /// dictionary.
        /// This was happening only because the tests create a new instance 
        /// of <see cref="CodeColorizer"/> instead of reusing a singleton and under dotnet core there 
        /// are no AppDomains and isolation by default or maybe the default for the parallelism changed 
        /// in the updated version of xunit. 
        /// I did choose to fix this instead of trying to find a way to change the test isolation 
        /// mode or the disable the parallelism.
        /// This ensure that if the user use the parameterless constructor things work correctly.
        /// If the user use the other constructor is resposible to provide a <see cref="ILanguageParser "/> 
        /// instance that is constructed as a singleton object graph.
        /// </summary>
        private static ILanguageCompiler languageCompiler = new LanguageCompiler(Languages.CompiledLanguages);

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeColorizer"/> class.
        /// </summary>
        public CodeColorizer()
        {
            languageParser = new LanguageParser(languageCompiler, Languages.LanguageRepository);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeColorizer"/> class.
        /// </summary>
        /// <param name="languageParser">The language parser that the <see cref="CodeColorizer"/> instance will use for its lifetime.</param>
        public CodeColorizer(ILanguageParser languageParser)
        {
            Guard.ArgNotNull(languageParser, "languageParser");
            
            this.languageParser = languageParser;
        }

        /// <summary>
        /// Colorizes source code using the specified language, the default formatter, and the default style sheet.
        /// </summary>
        /// <param name="sourceCode">The source code to colorize.</param>
        /// <param name="language">The language to use to colorize the source code.</param>
        /// <returns>The colorized source code.</returns>
        public string Colorize(string sourceCode, ILanguage language)
        {
            var buffer = new StringBuilder(sourceCode.Length * 2);

            using (TextWriter writer = new StringWriter(buffer))
            {
                Colorize(sourceCode, language, writer);

                writer.Flush();
            }

            return buffer.ToString();
        }

        /// <summary>
        /// Colorizes source code using the specified language, the default formatter, and the default style sheet.
        /// </summary>
        /// <param name="sourceCode">The source code to colorize.</param>
        /// <param name="language">The language to use to colorize the source code.</param>
        /// <param name="textWriter">The text writer to which the colorized source code will be written.</param>
        public void Colorize(string sourceCode, ILanguage language, TextWriter textWriter)
        {
            Colorize(sourceCode, language, Formatters.Default, StyleSheets.Default, textWriter);
        }

        /// <summary>
        /// Colorizes source code using the specified language, formatter, and style sheet.
        /// </summary>
        /// <param name="sourceCode">The source code to colorize.</param>
        /// <param name="language">The language to use to colorize the source code.</param>
        /// <param name="formatter">The formatter to use to colorize the source code.</param>
        /// <param name="styleSheet">The style sheet to use to colorize the source code.</param>
        /// <param name="textWriter">The text writer to which the colorized source code will be written.</param>
        public void Colorize(string sourceCode,
                             ILanguage language,
                             IFormatter formatter,
                             IStyleSheet styleSheet,
                             TextWriter textWriter)
        {
            Guard.ArgNotNull(language, "language");
            Guard.ArgNotNull(formatter, "formatter");
            Guard.ArgNotNull(styleSheet, "styleSheet");
            Guard.ArgNotNull(textWriter, "textWriter");

            formatter.WriteHeader(styleSheet, language, textWriter);

            languageParser.Parse(sourceCode, language, (parsedSourceCode, captures) => formatter.Write(parsedSourceCode, captures, styleSheet, textWriter));

            formatter.WriteFooter(styleSheet, language, textWriter);
        }
    }
}