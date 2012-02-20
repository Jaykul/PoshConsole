// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IPSWpfOptions.cs" company="Huddled Masses.org">
//   Copyright (c) 2010-2012 Joel Bennett
// </copyright>
// <summary>
//   <para>Provides an interface which extends the existing PowerShell PrivateData class with a
//   <see cref="IPSWpfConsole" />, with access to the WPF Window and Dispatcher</para>
// </summary>
// --------------------------------------------------------------------------------------------------------------------

// ReSharper disable CheckNamespace
namespace System.Management.Automation.Host
{
   /// <summary>
   /// <para>Provides an interface which extends the existing PowerShell PrivateData class with a
   /// <see cref="IPSWpfConsole" />, with access to the WPF Window and Dispatcher</para>
   /// </summary>
   public interface IPSWpfOptions
   {
      /// <summary>
      /// Gets WpfConsole.
      /// </summary>
      IPSWpfConsole WpfConsole { get; }
   }
}
// ReSharper restore CheckNamespace