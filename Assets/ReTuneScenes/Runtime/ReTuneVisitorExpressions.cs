using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ReTune.Scenes
{
    public sealed class ReTuneVisitorExpressions : MonoBehaviour
    {
        [Serializable] public sealed class Visitor
        {
            public Image portrait;
            public Sprite[] expressions;
        }
        public Visitor[] visitors;
        readonly Dictionary<int, int> pageExpressions = new Dictionary<int, int>();
        int activeVisitor = -1, lastPage = -1, lastExpression;

        public void Show(int customer, int page)
        {
            if (visitors == null || customer < 0 || customer >= visitors.Length) return;
            var visitor = visitors[customer];
            if (visitor == null || visitor.portrait == null || visitor.expressions == null || visitor.expressions.Length == 0) return;
            if (activeVisitor != customer)
            {
                activeVisitor = customer; lastPage = -1; lastExpression = 0; pageExpressions.Clear();
            }
            if (page == lastPage) return;
            if (!pageExpressions.TryGetValue(page, out int expression))
            {
                expression = 0;
                if (page > 0 && visitor.expressions.Length > 1)
                {
                    expression = UnityEngine.Random.Range(0, visitor.expressions.Length - 1);
                    if (expression >= lastExpression) expression++;
                }
                pageExpressions.Add(page, expression);
            }
            if (visitor.expressions[expression] != null) visitor.portrait.sprite = visitor.expressions[expression];
            // Sprite crops share the initial portrait's dimensions. Do not resize or reparent the seated image.
            lastPage = page; lastExpression = expression;
        }
    }
}
