// SPDX-FileCopyrightText: 2013 smdn <smdn@smdn.jp>
// SPDX-License-Identifier: MIT

using System;

using CategorySet = System.Collections.Generic.HashSet<string>;

namespace Smdn.HatenaBlogTools;

internal class CategoryModification {
  public CategorySet Old { get; }
  public CategorySet New { get; }

  public CategoryModification(CategorySet old, CategorySet @new)
  {
    Old = old ?? throw new ArgumentNullException(nameof(old));
    New = @new ?? throw new ArgumentNullException(nameof(old));
  }

  public void Apply(CategorySet categories)
  {
    if (Old.Count == 0) {
      if (categories.Count == 0)
        // カテゴリが設定されていない場合、新規設定
        categories.UnionWith(New);
    }
    else if (categories.IsSupersetOf(Old)) {
      // カテゴリがすべて設定されている場合、置換または削除
      categories.ExceptWith(Old);
      categories.UnionWith(New);
    }
  }
}
