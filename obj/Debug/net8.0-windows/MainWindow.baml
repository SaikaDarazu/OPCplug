   M S B A M L   `   `   `  ����B  >OPCplug, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null!clr-namespace:OPCplugOPCplug  5   	       OPCplug.MainWindow   5   	   W SPresentationCore, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=8.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35F 9http://schemas.microsoft.com/winfx/2006/xaml/presentation     5   	   R NSystem.Xaml, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   'localclr-namespace:OPCplug     5   	     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)   .��5
      � $��10q�6    � Cursor$  ��6    � ColumnDefinitions 5   
   �� 5      $
��auto �6   �� 5      $
��auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $
��auto �6   �� 5      $��* �6   .+�5   
   �� $��0��6   $��0��6"    �� Orientation$ Vertical=�62   $
��Left=�6I   $��0,30,0,0q�6d   .+�5      �� -   5        $��EscribirMemoriaBoton��5      $��Escribe Memoria��61      ControlButtonsStyle#��[  5      5      �� 5       -   5        $��LeerMemoriaBoton��5       $��Leer Memoria��6-   #��[  5!      5"      5#      �� 5&   
   $��0��6   $��1��6"   $ Vertical=�62   $
��Left=�6I   $��0,30,0,0q�6d   .+�5*      �� -   5         ��Name$ WritableField��5*        WritableFieldStyle#��[ 5+      �� 5-      -   5         ��Name$ ReadOnlyField��5-      $��Valor de solo lectura��6/     ReadOnlyFieldStyle#��[ 5.      51      �� 57   
   -   5        $��TextBoxLogs��57   F   $��1��6   " �� VerticalScrollBarVisibility$
 Auto=�6#   $��10,5,0,0q�58       �� 
IsReadOnly$
 True��6$   $  ��66   $��3��6@   $	��Top=�6T   59      5:      