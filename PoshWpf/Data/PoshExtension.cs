#if CLR_V4
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;
using System.Windows;
using System.Windows.Markup;
using System.Xaml;
using PoshWpf.Utility;

namespace PoshWpf.Data
{
   [MarkupExtensionReturnType(typeof(RoutedEventHandler))]
   public class PoshExtension : MarkupExtension
   {
      public string Code { get; set; }

      public PoshExtension(string code)
      {
         Code = code;
      }

      public override object ProvideValue(IServiceProvider serviceProvider)
      {
         var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
         var root = serviceProvider.GetService(typeof(IRootObjectProvider)) as IRootObjectProvider;
         var element = target !=null ? target.TargetObject as UIElement : null;

         //(ICollection<PSObject>) target.Dispatcher.Invoke(((Func<PSVariable[], Object[], ICollection<PSObject>>) Invoker), vars, Parameters)

         var variables = new[]
         {
             new PSVariable("this",(target != null) ? target.TargetObject : ((root != null) ? root.RootObject : null)),
             new PSVariable("window", (root != null) ? root.RootObject : null),
             new PSVariable("PowerBootsModule", Invoker.Module)
         };
         // Invoker.SetScriptVariableValue("PowerBootsModule", Invoker.Module);
         ICollection<PSObject> collection;

         if(element != null)
         {
            collection = (ICollection<PSObject>) element.Dispatcher.Invoke(
               ((Func<ScriptBlock, PSVariable[], ICollection<PSObject>>)Invoker.Invoke),
               ScriptBlock.Create( "[system.windows.routedeventhandler]{&{" + Code + " @args} $this $_ @args}"), variables);
         } else
         {
            collection = Invoker.Invoke(
               "[system.windows.routedeventhandler]{&{" + Code + " @args} $this $_ @args}", variables);
         }

         return collection.First().BaseObject as RoutedEventHandler;
      }
   }
}
#endif