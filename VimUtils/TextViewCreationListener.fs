namespace ViEmu
    open System
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open Microsoft.VisualStudio
    open Microsoft.VisualStudio.OLE.Interop
    open Microsoft.VisualStudio.Utilities
    open Microsoft.VisualStudio.Editor
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.TextManager.Interop
    open System.ComponentModel.Composition


    [<Export(typeof<IVsTextViewCreationListener>)>]
    [<ContentType("text")>]
    [<TextViewRole(PredefinedTextViewRoles.Editable)>]
    type TextViewCreationListener() =
        [<Import>]
        let mutable editorFactory: IVsEditorAdaptersFactoryService = null

        let mutable cursorBlockLayer: AdornmentLayerDefinition = null

        [<Export(typeof<AdornmentLayerDefinition>)>]
        [<Name("CursorBlockLayer")>]
        [<Order(After= PredefinedAdornmentLayers.Selection, Before = PredefinedAdornmentLayers.Text)>]
        [<TextViewRole(PredefinedTextViewRoles.Editable)>]
        member this.CursorBlockLayer with get() = cursorBlockLayer
                                        and set(value) = cursorBlockLayer <- value
        
        

        interface IVsTextViewCreationListener with
            member this.VsTextViewCreated textViewAdapter =
                let wpfTextView = editorFactory.GetWpfTextView textViewAdapter
                if wpfTextView <> null && wpfTextView.Properties.ContainsProperty typeof<ViKeyProcessor> then
                    let processor = wpfTextView.Properties.GetProperty<ViKeyProcessor> typeof<ViKeyProcessor>
                    let commandFilter = CommandFilter(processor, textViewAdapter)
                    let result, nextCommand = textViewAdapter.AddCommandFilter(commandFilter)
                    if result = VSConstants.S_OK then
                        commandFilter.NextTarget <- Some(nextCommand) 
                        wpfTextView.Properties.AddProperty(typeof<CommandFilter>, commandFilter)

