namespace ViEmu.Modes
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    open ViEmu.Interfaces

    type BaseMode() =
        let (|UpperCase|LowerCase|) key =
            if Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) then
                UpperCase
            else
                LowerCase
        
        interface IViMode<IViContext> with
            member this.HandleKey context args =
                //if key isn't handled the inheriting class should set args.Handled to false
                args.Handled <- true 
                match args.Key with
                    |UpperCase -> this.HandleUpperCase(context, args)
                    |LowerCase -> this.HandleLowerCase(context, args)

            member this.HandleTab context = 
                //TODO- context.Operations.Indent()
                false
            
            member this.HandleEnter context = 
                false

        abstract member HandleLowerCase: IViContext * KeyEventArgs -> unit
        default this.HandleLowerCase(context, args) = 
            match args.Key with
                    |Key.I -> context.SetInsertMode()
                    |Key.A -> 
                        context.Operations.GoToNextChar()
                        context.SetInsertMode()
                    |Key.H -> context.Operations.GoToPrevChar()
                    |Key.L -> context.Operations.GoToNextChar()
                    |Key.J -> context.Operations.GoToNextLine()
                    |Key.K -> context.Operations.GoToPrevLine()
                    |Key.B -> context.Operations.GoToPrevWord(includePunctuation=true)
                    |Key.W -> context.Operations.GoToNextWord(includePunctuation=true)
                    |Key.D0 -> context.Operations.GoToLineStart()
                    |Key.X -> context.Operations.DeleteCharacter()
                    |Key.R -> context.SetOverwriteMode(singleChar=true)
                    |Key.D -> context.SetDeleteMode()
                    |Key.Y -> context.SetYankMode()
                    |Key.P -> context.Operations.PasteBelow()
                    |Key.O -> 
                        context.Operations.OpenLineBelow()
                        context.SetInsertMode()
                    |Key.V -> context.SetVisualMode()
                    |Key.U -> context.Undo()
                    |_ -> args.Handled <- false
            
        abstract member HandleUpperCase: IViContext * KeyEventArgs -> unit
        default this.HandleUpperCase(context, args) =
            match args.Key with
                |Key.D -> 
                    context.Operations.GoToLineEnd(extendSelection=true)
                    context.Operations.DeleteSelection() |> ignore
                |Key.Y -> context.Operations.CopyToLineEnd() |> ignore
                |Key.B -> context.Operations.GoToPrevWord(includePunctuation=false)
                |Key.W -> context.Operations.GoToNextWord(includePunctuation=false)
                |(* Key.$ *) Key.D4-> context.Operations.GoToLineEnd()
                |Key.J -> context.Operations.JoinPreviousLine()
                |Key.R -> context.SetOverwriteMode(singleChar=false)
                |Key.O -> 
                    context.Operations.OpenLineAbove()
                    context.SetInsertMode()
                |Key.P -> context.Operations.PasteAbove()
                |Key.A -> 
                    context.Operations.GoToLineEnd()
                    context.SetInsertMode()
                |Key.U -> context.Redo()
                |_ -> args.Handled <- false

