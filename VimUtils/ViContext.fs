namespace ViEmu
    open Microsoft.VisualStudio
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations

    open ViEmu.Interfaces
    open ViEmu.Modes
    open ViEmu.CursorAdornment
    
    type Context(_mode: IViMode<IViContext> option, textView: IWpfTextView, ops: #IEditorOperations, undoHistRegistry: ITextUndoHistoryRegistry) =
        
        let mutable mode = _mode
        let textOps = new TextOperations.Operations(textView, ops) :> IViTextOperations
        
        let adornment = new CursorAdornment(textView)
        //do textOps.CaretMoved.Add (fun caret -> adornment.DrawCursor() )

        let overwrite choice =
                textView.Options.SetOptionValue("TextView/OverwriteMode", choice)

        let adapt (mode: #IViMode<IViContext>) = 
            Some(mode :> IViMode<IViContext>)

        interface IViContext with
            member this.Mode with get() = mode 
            member this.TextView with get() = textView 
            member this.Operations with get() = textOps
            
            member this.Undo () = undoHistRegistry.RegisterHistory(textView.TextBuffer).Undo(1)
            member this.Redo () = undoHistRegistry.RegisterHistory(textView.TextBuffer).Redo(1)

            member this.SetBaseMode () =
                overwrite true
                mode <- adapt <| BaseMode()

            member this.SetOverwriteMode singleChar =
                let single = defaultArg singleChar false
                mode <- adapt <| OverwriteMode(single)
                
            member this.SetDeleteMode () =
                mode <- adapt <| DeleteMode() 
                
            member this.SetYankMode () =
                mode <- adapt <| CopyMode()
                
            member this.SetInsertMode () =
                overwrite false
                mode <- None

            member this.SetVisualMode () =
                mode <- adapt <| VisualMode()

            member this.SetFindMode () =
                mode <- adapt <| FindMode()
                  
                




