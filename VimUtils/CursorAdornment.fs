namespace ViEmu 
    

module CursorAdornment =
    open System
    open System.Collections.Generic
    open System.ComponentModel.Composition
    open System.Windows.Controls
    open System.Windows.Media
    open Microsoft.VisualStudio
    open Microsoft.VisualStudio.OLE.Interop
    open Microsoft.VisualStudio.Utilities
    open Microsoft.VisualStudio.Editor
    open Microsoft.VisualStudio.Text
    open Microsoft.VisualStudio.Text.Editor
    open Microsoft.VisualStudio.TextManager.Interop
    open System.ComponentModel.Composition

    type CursorAdornment(textView: IWpfTextView) =
        let layer = textView.GetAdornmentLayer("CursorBlockLayer")
        let brush = new SolidColorBrush(Color.FromArgb(byte 0x33, byte 0x33, byte 0x33, byte 0x88))
        let penBrush = new SolidColorBrush(Colors.Black)
        let pen = new Pen(penBrush, 0.5)
        
        do
            brush.Freeze()
            penBrush.Freeze()
            pen.Freeze()
        
        let mutable oldShape: Geometry = new RectangleGeometry() :> Geometry
        let mutable image: Image = new Image()

        let getVirtualSpan () = 
            let pos = textView.Caret.Position.BufferPosition
            let current = textView.Caret.Position.VirtualBufferPosition
            let next = new VirtualSnapshotPoint(pos, current.VirtualSpaces + 1)
            let virt = new VirtualSnapshotSpan(current, next)
            virt.SnapshotSpan           

        let getSpan () =
            let current = textView.Caret.Position.BufferPosition
            let next = current.Add(1)
            new SnapshotSpan(current, next)

        let drawCursor () =
            layer.RemoveAllAdornments()
            let posCurrent = textView.Caret.Position.BufferPosition
            let span = if posCurrent.Position >= posCurrent.Snapshot.Length || textView.Caret.InVirtualSpace then
                            getVirtualSpan ()
                        else
                           getSpan ()

            let line = textView.Caret.ContainingTextViewLine
            let bounds = line.GetCharacterBounds(textView.Caret.Position.VirtualBufferPosition)
            let shape = new RectangleGeometry(new Windows.Rect(bounds.Left, bounds.TextTop, bounds.Width, bounds.TextHeight))

            if shape = null then () else
            if oldShape.Bounds <> shape.Bounds then
                oldShape <- shape
                let drawing = new GeometryDrawing(brush, pen, shape)
                drawing.Freeze()
                let drawingImage = new DrawingImage(drawing)
                drawingImage.Freeze()
                image <- new Image()
                image.Source <- drawingImage
            Canvas.SetLeft(image, shape.Bounds.Left)
            Canvas.SetTop(image, shape.Bounds.Top)
            layer.AddAdornment(AdornmentPositioningBehavior.TextRelative, Nullable(span), null, image, null) |> ignore
        
        
        let layoutChanged evt =  drawCursor ()

        do 
            textView.LayoutChanged.Add (fun evt -> layoutChanged evt)
            textView.Caret.PositionChanged.Add (fun evt -> layoutChanged evt)

        member this.DrawCursor () = drawCursor ()
            
            

