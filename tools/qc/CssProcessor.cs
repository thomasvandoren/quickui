﻿using System;
using System.Text.RegularExpressions;

namespace qc
{
    /// <summary>
    /// Helper class which can generate the CSS for a control.
    /// </summary>
    public static class CssProcessor
    {
        public static string CssForClass(MarkupControlClass c)
        {
            if (c.Style == null)
            {
                return String.Empty;
            }

            string styles = c.Style.Trim();
            return styles.Length == 0
                ? String.Empty
                : Template.Format(
                    "/*** {ClassName} ***/\n\n{Styles}\n\n",
                    new
                    {
                        ClassName = c.Name,
                        Styles = AddClassNameToSelectors(c.Name, styles)
                    });
        }

        /// <summary>
        /// Prepend the class name to all style selectors.
        /// </summary>
        /// <remarks>
        /// If the class name is "Foo", this will find all rules in the CSS like
        /// "p { color: gray; }" and change them to ".Foo p { color: gray; }".
        /// This ensures the styles will only be applied to the DOM within a control,
        /// and avoids style name conflicts with styles defined by other controls
        /// in use on the same page.
        /// 
        /// Exception: if the selector already begins with .Foo, then the the class name
        /// is *not* prepended. This allows for the definition of rules like ".Foo.Bar".
        /// </remarks>
        public static string AddClassNameToSelectors(string className, string styles)
        {
            // We could do a simple findClassNames.Replace() if it weren't for the
            // exception about not prepending the class name to the rule if the
            // class name is already there. So, we'll do a manual match-and-replace.
            MatchCollection matches = findSelectorsRegEx.Matches(styles);

            // Walk backward through the matches and captures so the match indexes
            // stay valid during the loop.
            string cssClassName = "." + className;
            for (int i = matches.Count - 1; i >= 0; i--)
            {
                CaptureCollection ruleCaptures = matches[i].Groups[1].Captures;
                for (int j = ruleCaptures.Count - 1; j >= 0; j--)
                {
                    Capture capture = ruleCaptures[j];

                    // Ignore selectors that meet any of the following conditions:
                    //  1) the style name is already included,
                    //  2) the selector is a CSS directive (starts with "@").
                    if (!capture.Value.StartsWith(cssClassName) &&
                        !capture.Value.StartsWith("@"))
                    {
                        // Selector doesn't already start with class name, so prepend it.
                        styles = styles.Insert(capture.Index, cssClassName + " ");
                    }
                }
            }

            return styles;
        }

        // Regex to find selectors in CSS.
        private const string findSelectors =
            @"(?:/\*.*?\*/\s*)*" +              // Absorb comments
            @"(?<selector>.*?)\s*" +            // First selector
            @"(?:,\s*(?<selector>.*?)\s*)*" +   // Comma separated remaining selector
            @"\{[^\}]*\}";                      // Style
        private static Regex findSelectorsRegEx = new Regex(findSelectors, RegexOptions.Compiled);
    }
}
