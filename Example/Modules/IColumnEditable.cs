namespace Example.Modules;

// 一覧グリッド部品（Parts）がバインドするViewModelの契約。
public interface IColumnEditable
{
    ICommand SelectCommand { get; }

    ICommand ColumnEditCommand { get; }

    // 列の表示と順序。グリッドが正規化した値をTwoWayで書き戻す
    IReadOnlyList<GridColumnOrder>? ColumnOrders { get; set; }

    GridSelectRequest GridSelectRequest { get; }
}
