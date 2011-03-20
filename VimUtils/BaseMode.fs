namespace ViEmu.Modes
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor
    open ViEmu.Interfaces

    type BaseMode(extendSelection: bool) =
        let (|UpperCase|LowerCase|) key =
            if Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) then
                UpperCase
            else
                LowerCase

        new () = BaseMode(false)
        
        interface IViMode<IViContext> with
            member this.HandleKey context args =
                //if key isn't handled the inheriting class should set args.Handled to false
                args.Handled <- true 
                match args.Key with
                    |UpperCase -> this.HandleUpperCase(context, args)
                    |LowerCase -> this.HandleLowerCase(context, args)

            // return true to indicate that the key has been handled, stops further propogation of the event 
            member this.HandleTab context = 
                match true with
                 | UpperCase -> context.Operations.Unindent()
                 | LowerCase -> context.Operations.Indent()
                true
            
            member this.HandleEnter context = 
                false

        abstract member HandleLowerCase: IViContext * KeyEventArgs -> unit
        default this.HandleLowerCase(context, args) = 
            match args.Key with
                    |Key.I -> context.SetInsertMode()
                    |Key.A -> 
                        context.Operations.GoToNextChar()
                        context.SetInsertMode()
                    |Key.H | Key.Left -> context.Operations.GoToPrevChar(extendSelection)
                    |Key.L | Key.Right -> context.Operations.GoToNextChar(extendSelection)
                    |Key.J | Key.Down -> context.Operations.GoToNextLine(extendSelection)
                    |Key.K | Key.Up -> context.Operations.GoToPrevLine(extendSelection)
                    |Key.B -> context.Operations.GoToPrevWord(includePunctuation=true, extendSelection=extendSelection)
                    |Key.W -> context.Operations.GoToNextWord(includePunctuation=true, extendSelection=extendSelection)
                    |Key.D0 -> context.Operations.GoToLineStart(extendSelection)
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
                    |Key.Tab -> context.Operations.Indent()
                    |_ -> args.Handled <- false
            
        abstract member HandleUpperCase: IViContext * KeyEventArgs -> unit
        default this.HandleUpperCase(context, args) =
            match args.Key with
                |Key.D -> 
                    context.Operations.GoToLineEnd(extendSelection=true)
                    context.Operations.DeleteSelection() |> ignore
                |Key.Y -> context.Operations.CopyToLineEnd() |> ignore
                |Key.B -> context.Operations.GoToPrevWord(includePunctuation=false, extendSelection=extendSelection)
                |Key.W -> context.Operations.GoToNextWord(includePunctuation=false, extendSelection=extendSelection)
                |(* Key.$ *) Key.D4-> context.Operations.GoToLineEnd(extendSelection)
                |Key.J -> context.Operations.JoinNextLine()
                |Key.R -> context.SetOverwriteMode(singleChar=false)
                |Key.O -> 
                    context.Operations.OpenLineAbove()
                    context.SetInsertMode()
                |Key.P -> context.Operations.PasteAbove()
                |Key.A -> 
                    context.Operations.GoToLineEnd()
                    context.SetInsertMode()
                |Key.U -> context.Redo()
                |(* Key.{ *) Key.OemOpenBrackets -> context.Operations.GoToParagraphStart(extendSelection)
                |(* Key.} *) Key.OemCloseBrackets -> context.Operations.GoToParagraphEnd(extendSelection)
                |Key.Tab -> context.Operations.Indent()
                |_ -> args.Handled <- false

