// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Configuration
{
#pragma warning disable AS0003  // Allow sealed attribute

    /// <summary>
    /// Attribute used to adorn configuration objects.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public sealed class ConfigurationAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the configuration file.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets a value indicating whether or not the file is optional.
        /// </summary>
        public bool Optional { get; }

        /// <summary>
        /// Check whether or not the FileName is set.
        /// </summary>
        public bool FileNameSet => !string.IsNullOrEmpty(FileName);

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigurationAttribute"/> class.
        /// </summary>
        /// <param name="fileName">Optional name of the configuration file.</param>
        /// <param name="optional">Whether or not the file is optional - default = false.</param>
        public ConfigurationAttribute(string fileName = "", bool optional = false)
        {
            FileName = fileName;
            Optional = optional;
        }
    }
}
