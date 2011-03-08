﻿
module ViEmu.TextOperations
    open System
    open System.Windows.Input
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.Text.Operations
    open Microsoft.VisualStudio.Editor

    open Interfaces

    type internal OverwriteOff(textView: #IWpfTextView) =
        let view = textView :> IWpfTextView
        do
            view.Options.SetOptionValue("TextView/OverwriteMode", false)

        interface IDisposable with
            member this.Dispose () = view.Options.SetOptionValue("TextView/OverwriteMode", true)


    type internal CharMovementContext(textView: #IWpfTextView, ops: #IEditorOperations, includePunc: bool) =
        let (|Whitespace|Symbol|Alpha|) khar =
            if Char.IsWhiteSpace khar then Whitespace else
            if Char.IsLetterOrDigit khar then Alpha else
            Symbol

        let stateChange previous current = 
            match previous, current with
            |Whitespace, Whitespace
            |Alpha, Alpha
            |Symbol, Symbol -> false
            |Whitespace, _ 
            |Alpha, Whitespace 
            |Symbol, Whitespace -> true
            |Alpha, Symbol
            |Symbol, Alpha when includePunc -> true
            |Alpha, Symbol
            |Symbol, Alpha -> false //when not includePunc

        let mutable point = textView.Caret.Position.BufferPosition
        let nextChar () = point <- point.Add(1)
        let prevChar () = point <- point.Subtract(1)

        let move direction =
            let previous = point.GetChar()
            direction()
            let current = point.GetChar()
            stateChange previous current

        member this.MoveToNextChar () = move nextChar

        member this.MoveToPrevChar () = move prevChar

        member this.SetCaretPosition extendSelection = 
            ops.SelectAndMoveCaret(textView.Caret.Position.VirtualBufferPosition, VirtualSnapshotPoint(point))
            if not extendSelection then ops.ResetSelection()

        member this.InWhitespace () = Char.IsWhiteSpace (point.GetChar())

    let flip (fxn: 'a -> 'b -> 'c) (x: 'b) (y: 'a) = fxn y x

    type Operations(textView: #IWpfTextView, ops: #IEditorOperations) =
        let passResultOver (x: 'a) (fxn: unit -> unit) = 
            fxn()
            x

        //convert boolean default argument to false if not given
        let (!!) = flip defaultArg false

        let maybeSetClipboard (input: string option) =
            match input with
                | Some(str) -> System.Windows.Clipboard.SetText(str)
                | None -> ()

        let withRelativePositionMaintained fxn =
            let trackingPoint = textView.TextSnapshot.CreateTrackingPoint(
                                                        textView.Caret.Position.BufferPosition.Position, 
                                                        PointTrackingMode.Negative)
            let res = fxn()
            textView.Caret.MoveTo(trackingPoint.GetPoint(textView.TextSnapshot)) |> ignore
            res 

        let pasteAndAdjust (content: string option) =
            let text = defaultArg content (System.Windows.Clipboard.GetText())
            let trimmed = text.Trim()
            withRelativePositionMaintained <| fun () ->
                use off = new OverwriteOff(textView)
                ops.InsertText(trimmed) |> ignore
                if text.[text.Length - 1] = '\n' then 
                    ops.InsertNewLine() |> ignore
        
        let setIndentation xpos =
            let snapShotPt = textView.Caret.ContainingTextViewLine.GetVirtualBufferPositionFromXCoordinate(xpos)
            let whiteSpace = ops.GetWhitespaceForVirtualSpace(snapShotPt)
            use off = new OverwriteOff(textView)
            ops.InsertText whiteSpace
            
                    

        let moveHorzIfNeeded () =
            let caret = textView.Caret
            let scroller = textView.ViewScroller
            if caret.Right > textView.ViewportRight then
                scroller.ScrollViewportHorizontallyByPixels(caret.Right - textView.ViewportRight)
            elif caret.Left < textView.ViewportLeft then
                scroller.ScrollViewportHorizontallyByPixels(caret.Left - textView.ViewportLeft)

        let moveVertIfNeeded () =
            let caret = textView.Caret
            let scroller = textView.ViewScroller
            if caret.Top < textView.ViewportTop then
                //counterintuitavely scrolling positive pixels moves up
                scroller.ScrollViewportVerticallyByPixels(textView.ViewportTop - caret.Top)     
            elif caret.Bottom > textView.ViewportBottom then
                scroller.ScrollViewportVerticallyByPixels(textView.ViewportBottom - caret.Bottom)

        let caretMoved = Event<_>()
        let triggerCaretMoved () = caretMoved.Trigger textView.Caret

        let ensureCaretIsVisible =
            //moveHorzIfNeeded >> moveVertIfNeeded
            textView.Caret.EnsureVisible 

        let wrapUp = ensureCaretIsVisible >> triggerCaretMoved

        let paragraphDivision check move finalMove =
            while textView.Caret.ContainingTextViewLine.Length <> 0 && not <| check() do
                move()
            finalMove()
            
        let paragraphEnd (extend: bool) =
            let check () = 
                let lineNum = textView.VisualSnapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition.Position)    
                lineNum >= (textView.VisualSnapshot.LineCount - 1)
            let move () = ops.MoveLineDown(extend)
            let finalMove () = ops.MoveToEndOfLine(extend)
            paragraphDivision check move finalMove

        let paragraphStart (extend: bool) =
            let check () =
                let lineNum = textView.VisualSnapshot.GetLineNumberFromPosition(textView.Caret.Position.BufferPosition.Position)    
                lineNum <= 0
            let move () = ops.MoveLineUp(extend)
            let finalMove () = ops.MoveToStartOfLineAfterWhiteSpace(extend)
            paragraphDivision check move finalMove

        let isInWord (includePunctuation: bool) =
                if includePunctuation then
                    not << System.Char.IsWhiteSpace
                else
                    System.Char.IsLetterOrDigit

        let goToWordPart includePunctuation extendSelection forward backward =
                let movementContext = new CharMovementContext(textView, ops, !!includePunctuation)
                if not <| movementContext.InWhitespace() then
                    let changedState = ref <| forward movementContext
                    while not !changedState && not (movementContext.InWhitespace()) do
                        changedState := forward movementContext
                    //will go one too far, back up
                    backward movementContext |> ignore
                    movementContext.SetCaretPosition(!!extendSelection)
                    //only need to wrap up if caret has moved
                    |>wrapUp

        let prevChar (movementContext: CharMovementContext) = movementContext.MoveToPrevChar()
        let nextChar (movementContext: CharMovementContext) = movementContext.MoveToNextChar()

        member this.CaretMoved = caretMoved.Publish

        interface IViTextOperations with
            member this.CaretMoved = caretMoved.Publish

            member this.GoToNextChar (?extendSelection) = ops.MoveToNextCharacter(!!extendSelection) |> wrapUp
            member this.GoToPrevChar (?extendSelection) = ops.MoveToPreviousCharacter(!!extendSelection) |> wrapUp
            
            member this.GoToLine (lineNum, ?extendSelection) =
                let position = textView.Caret.Position.BufferPosition.Position 
                if lineNum > 0 && lineNum < textView.TextViewLines.Count then
                     ops.GotoLine lineNum
                     ops.MoveToStartOfLineAfterWhiteSpace(false)
                     if !!extendSelection then
                        ops.ExtendSelection position
                        ops.SwapCaretAndAnchor()
                     ops.ScrollLineCenter() |> moveHorzIfNeeded |> triggerCaretMoved
            
            member this.GoToLineStart (?includeWhitespace, ?extendSelection) =
                match includeWhitespace with
                    |Some(true) -> ops.MoveToStartOfLine(!!extendSelection)
                    |Some(false) | None -> ops.MoveToStartOfLineAfterWhiteSpace(!!extendSelection)
                moveHorzIfNeeded() |> triggerCaretMoved

            member this.GoToLineEnd (?includeWhitespace) = 
                ops.MoveToEndOfLine(!!includeWhitespace) 
                |> moveHorzIfNeeded |> triggerCaretMoved

            member this.GoToPrevLine (?includeWhitespace) = ops.MoveLineUp(!!includeWhitespace) |> wrapUp

            member this.GoToNextLine (?includeWhitespace) = ops.MoveLineDown(!!includeWhitespace) |> moveVertIfNeeded |> triggerCaretMoved

            //whether state changes depends on the value of includePunctuation, when we do include punctuation
            //we change when going from alpha to symbol (or vice versa) and from whitespace to
            //non-whitespace.  When not including puctuation we only state change when going from whitespace
            //to non-whitespace
            member this.GoToPrevWord (?includePunctuation, ?extendSelection) =
                let movementContext = new CharMovementContext(textView, ops, !!includePunctuation)
                movementContext.MoveToPrevChar() |> ignore
                let changedState = ref <| movementContext.MoveToPrevChar()
                while not !changedState do
                    changedState := movementContext.MoveToPrevChar()
                movementContext.MoveToNextChar() |> ignore
                movementContext.SetCaretPosition(!!extendSelection)
                (* if we started in whitespace then we will still be in whitespace now, recursing one time will
                *  get us back into text *)
                if movementContext.InWhitespace() then
                    (this :> IViTextOperations).GoToPrevWord(!!includePunctuation, !!extendSelection)
                wrapUp()

            member this.GoToNextWord (?includePunctuation, ?extendSelection) =
                let movementContext = new CharMovementContext(textView, ops, !!includePunctuation)
                let changedState = ref <| movementContext.MoveToNextChar()
                while not !changedState do
                    changedState := movementContext.MoveToNextChar()
                movementContext.SetCaretPosition(!!extendSelection) |>ignore
                if movementContext.InWhitespace() then
                    (this :> IViTextOperations).GoToNextWord(!!includePunctuation, !!extendSelection)
                wrapUp()

            //does nothing if not currently inside a word and not already at the word start
            member this.GoToWordStart (?includePunctuation, ?extendSelection) =
                goToWordPart includePunctuation extendSelection prevChar nextChar 

            member this.GoToWordEnd (?includePunctuation, ?extendSelection) =
                goToWordPart includePunctuation extendSelection nextChar prevChar

            member this.GoToDocumentStart ?extendSelection =
                ops.MoveToStartOfDocument(!!extendSelection) |> wrapUp

            member this.GoToDocumentEnd ?extendSelection =
                ops.MoveToEndOfDocument(!!extendSelection) |> wrapUp

            member this.InsertNewLine () =
                ops.InsertNewLine() |> ignore
                |> wrapUp

            member this.OpenLineAbove () =
                use off = new OverwriteOff(textView)
                ops.MoveToStartOfLineAfterWhiteSpace(false)
                let left = textView.Caret.Left
                ops.MoveToStartOfLine(false)
                ops.InsertNewLine() |> ignore
                ops.MoveToPreviousCharacter(false)
                setIndentation(left) |> ignore
                |> wrapUp

            member this.OpenLineBelow () =
                use off = new OverwriteOff(textView)
                ops.MoveToHome(false)
                let left = textView.Caret.Left
                ops.MoveToStartOfNextLineAfterWhiteSpace(false)
                withRelativePositionMaintained (fun () -> ops.InsertNewLine() |> ignore)
                //ops.MoveToPreviousCharacter(false)
                //setIndentation(left) |> ignore
                |> wrapUp

            member this.CutSelection () =
                ops.CutSelection() |> ignore
                System.Windows.Clipboard.GetText()
                |> passResultOver <| wrapUp    

            //in vi deleting copies to clipboard
            member this.DeleteSelection () = (this :> IViTextOperations).CutSelection()

            member this.DeleteLine () =
                ops.ResetSelection()
                ops.CutFullLine() |> ignore
                System.Windows.Clipboard.GetText()
                |> passResultOver <| wrapUp

            member this.DeleteCharacter () =
                ops.ResetSelection()
                ops.Delete() |> ignore
                |> wrapUp                

            member this.CopyLine () =
                textView.Caret.EnsureVisible()
                ops.SelectLine(textView.Caret.ContainingTextViewLine, false)
                ops.CopySelection() |> ignore
                System.Windows.Clipboard.GetText()

            member this.PasteOverSelection ?content =
                if not <| textView.Selection.IsEmpty then
                    ops.Delete() |> ignore
                let text = defaultArg content (System.Windows.Clipboard.GetText())
                (this :> IViTextOperations).PasteInsert text

            member this.PasteInsert ?content =
                use off = new OverwriteOff(textView)
                ops.ResetSelection()
                maybeSetClipboard content
                ops.Paste() |> ignore
                |> wrapUp

            member this.PasteAbove ?content =
                ops.MoveToStartOfLineAfterWhiteSpace(false)
                pasteAndAdjust content
                |> wrapUp

            member this.PasteBelow ?content =
                use off = new OverwriteOff(textView)
                let text = defaultArg content (System.Windows.Clipboard.GetText())
                if text.Contains("\n") then
                    ops.MoveToStartOfNextLineAfterWhiteSpace(false)
                    pasteAndAdjust content
                    |> wrapUp
                else
                    let trimmed = text.Trim()
                    ops.InsertText trimmed |>ignore
                    |> wrapUp

            member this.JoinPreviousLine () =
                ops.MoveLineUp(false)
                ops.MoveToLastNonWhiteSpaceCharacter(false)
                ops.MoveToStartOfNextLineAfterWhiteSpace(true)
                ops.Delete() |> ignore
                |> wrapUp

            member this.CopySelection () =
                ops.CopySelection() |> ignore
                System.Windows.Clipboard.GetText()

            member this.ResetSelection () =
                ops.ResetSelection()

            member this.Indent () =
                withRelativePositionMaintained <| fun () ->
                    ops.IncreaseLineIndent() |> ignore
                |> wrapUp

            member this.Unindent() =
                withRelativePositionMaintained <| fun () ->
                    ops.Unindent() |> ignore
                |> wrapUp

            member this.CopyToLineEnd () =
                ops.MoveToEndOfLine(true) |> ignore
                ops.CopySelection() |> ignore
                ops.ResetSelection()
                System.Windows.Clipboard.GetText()
                |> passResultOver <| wrapUp

            member this.SelectEnclosingWord () =
                ops.SelectCurrentWord()
                |> wrapUp

            member this.GoToParagraphEnd (?extendSelection: bool) =
                paragraphEnd(!!extendSelection)
                |> wrapUp

            member this.GoToParagraphStart (?extendSelection: bool) =
                paragraphStart(!!extendSelection)
                |> wrapUp

            member this.SwapCaretAndAnchor () = ops.SwapCaretAndAnchor() |> wrapUp

            member this.AsSingleEvent fxn =
                //ops.AddBeforeTextBufferChangePrimitive()
                fxn()
                //ops.AddAfterTextBufferChangePrimitive()
                
