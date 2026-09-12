namespace ClamGrid;

/// <summary>
/// ソート入力の読み取り専用スナップショットと主キーから順の条件を受け取り、同じ行の安定した並べ替え結果を同期的に返す。
/// </summary>
/// <typeparam name="T">行の参照型。</typeparam>
/// <param name="rows">行オブジェクトを共有する入力順のスナップショット。</param>
/// <param name="orders">主キーから副キーまでの条件。</param>
/// <returns>全入力行を参照同一性でちょうど1回ずつ含む並べ替え結果。</returns>
public delegate IEnumerable<T> GridSortCallback<T>(IReadOnlyList<T> rows, IReadOnlyList<GridSortOrder> orders)
    where T : class;
