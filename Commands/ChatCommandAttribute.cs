using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace com.goodstuff.Starship.Commands
{
    /// <summary>
    /// Applying this attribute to an implementation of the CommandBase class will cause that class to be
    /// loaded at runtime.  The classes 'name' property is the keyword used to execute that command.  At
    /// this time, any command that duplicates an existing command name will override the preceding
    /// command.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class ChatCommandAttribute : Attribute
    {
    }
}
