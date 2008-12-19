using System;
using System.Collections.Generic;
using System.Text;
using System.Management.Automation;
using System.ComponentModel;

namespace PoshWpf
{
   [RunInstaller(true)]
   public class PoshWpfSnapIn : PSSnapIn
   {
      public override string Name
      {
         get { return "PoshWpf"; }
      }
      public override string Vendor
      {
          get { return "HuddledMasses.org"; }
      }
      public override string VendorResource
      {
         get { return "PoshWpf, HuddledMasses.org"; }
      }
      public override string Description
      {
         get { return "Cmdlets for working with WPF and implementing PowerBoots"; }
      }
      public override string DescriptionResource
      {
         get { return "PoshWpf, Registers the Cmdlets and Providers in this assembly"; }
      }
   }
}
