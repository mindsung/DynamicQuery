using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MindSung.DynamicQuery
{
  internal static class ExpressionHelpers
  {
    /// <summary>
    /// Returns the PropertyInfo on the specified type with the propertyName. Case-insensitive.
    /// Returns null if the property does not exist.
    /// </summary>
    /// <param name="type"></param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static PropertyInfo GetProperty(TypeInfo type, string propertyName)
    {
      return type.GetProperties().FirstOrDefault(p => p.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Creates an expression that checks if the source Expression is null. If so, null is returned, otherwise
    /// the result expression is returned.
    /// <para/>
    /// source == null ? null : result
    /// </summary>
    /// <param name="source"></param>
    /// <param name="result"></param>
    /// <returns></returns>
    public static Expression GetNullConditional(Expression source, Expression result)
    {
      result = MakeNullable(result);
      var sourceNull = Expression.Constant(null, source.Type);
      var propertyNull = Expression.Constant(null, result.Type);
      // source == (Source_Type)null
      var equalsNull = Expression.Equal(source, sourceNull);
      // source == (Source_Type)null ? (Result_Type)null : result
      result = Expression.Condition(equalsNull, propertyNull, result);
      return result;
    }

    /// <summary>
    /// If the type the given Expression returns is not already nullable, this converts it to a nullable type.
    /// </summary>
    /// <param name="e"></param>
    /// <returns></returns>
    public static Expression MakeNullable(Expression e)
    {
      if (e.Type.GetTypeInfo().IsValueType && Nullable.GetUnderlyingType(e.Type) == null)
      {
        return Expression.Convert(e, typeof(Nullable<>).MakeGenericType(e.Type));
      }

      return e;
    }

    /// <summary>
    /// Creates a constant expression that converts the given value using the "Parse" method of the
    /// supplied "convertType" if it has one, otherwise it will cast to the specified type.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="convertType"></param>
    /// <returns></returns>
    public static Expression TryGetValueType(object value, Type convertType)
    {
      var parse = convertType.GetTypeInfo().GetMethod("Parse", new [] { typeof(string) });
      if (parse != null)
      {
        return Expression.Constant(parse.Invoke(null, new [] { value.ToString() }), convertType);
      }
      else
      {
        return Expression.Convert(Expression.Constant(value), convertType);
      }
    }

    /// <summary>
    /// If the specified property refers to a static LambdaExpression property, this sets result to that expression with the source
    /// expression supplied as the lambda parameter.
    /// <para/>
    /// Example: i.SomeAggregateExpression
    /// </summary>
    /// <param name="propertyInfo">The property that may or may not contain a LambdaExpression.</param>
    /// <param name="source">The expression that will be given to the LambdaExpression as a parameter.</param>
    /// <param name="result">If successful, represents the body of a LambdaExpression with the source replacing the original LambdaExpression's parameter.</param>
    /// <returns></returns>
    private static bool TryGetExpressionProperty(PropertyInfo propertyInfo, Expression source, out Expression result)
    {
      if (propertyInfo == null)
      {
        throw new InvalidOperationException($"Property on '{source.Type.FullName}' is not valid");
      }
      var propertyTypeInfo = propertyInfo.PropertyType;
      if (!typeof(Expression).GetTypeInfo().IsAssignableFrom(propertyTypeInfo))
      {
        result = null;
        return false;
      }

      if (!typeof(LambdaExpression).GetTypeInfo().IsAssignableFrom(propertyTypeInfo))
      {
        throw new InvalidOperationException($"Expression properties must return a LambdaExpression. '{propertyInfo.DeclaringType.FullName}.{propertyInfo.Name}'");
      }

      var property = propertyInfo.GetAccessors().FirstOrDefault();
      if (property == null || !property.IsStatic)
      {
        throw new InvalidOperationException($"Expression properties must be static. '{propertyInfo.DeclaringType.FullName}.{propertyInfo.Name}'");
      }

      // j => j.SomeAggregateExpression
      var lambda = (LambdaExpression) property.Invoke(null, null);
      var changer = new ParamChanger(lambda.Parameters[0], source);
      // i.SomeAggregateExpression
      result = changer.Visit(lambda.Body);
      return true;
    }

    /// <summary>
    /// Returns an expression that accesses a simple property on some Type specified by the source expression.
    /// <para/>
    /// Example: source.SomeProperty 
    /// </summary>
    /// <param name="source">The Expression that accesses the object that contains the specified property.</param>
    /// <param name="propertyInfo">The property from the type that the Expression will access.</param>
    /// <returns></returns>
    public static Expression GetPropertyExpression(Expression source, PropertyInfo propertyInfo)
    {
      if (!TryGetExpressionProperty(propertyInfo, source, out var propGetter))
      {
        // i.SomeProperty1
        propGetter = Expression.Property(source, propertyInfo);
      }

      return propGetter;
    }

    /// <summary>
    /// Returns an expression that accesses a simple property on some Type specified by the source expression. This
    /// will support retrieving nested properties as well by specifying a propertyName containing '.' delimiters.
    /// <para/>
    /// Example: source.SomeProperty 
    /// </summary>
    /// <param name="source">The Expression that accesses the object that contains the specified property.</param>
    /// <param name="propertyName"></param>
    /// <returns></returns>
    public static Expression GetPropertyExpression(Expression source, string propertyName)
    {
      if (string.IsNullOrEmpty(propertyName))
      {
        throw new ArgumentException($"{nameof(propertyName)} cannot be null or empty.");
      }

      var props = propertyName.Split('.');
      var property = GetProperty(source.Type.GetTypeInfo(), props[0]);
      if (property == null)
      {
        throw new InvalidOperationException($"'{props[0]}' is not a valid property on '{source.Type.FullName}'");
      }
      var result = GetPropertyExpression(source, property);
      for (var i = 1; i < props.Length; i++)
      {
        var propertyInfo = result.Type.GetTypeInfo();
        property = GetProperty(propertyInfo, props[i]);
        if (property == null)
        {
          throw new InvalidOperationException($"The nested property '{props[i]}' within {propertyName} is not a valid property on '{source.Type.FullName}'");
        }
        result = GetPropertyExpression(result, property);
      }
      return result;
    }

    /// <summary>
    /// Returns true if the provided TypeInfo refers to a generic IEnumerable. In other words,
    /// IEnumerable&lt;string&gt; will return true, but string itself, while IEnumerable, will
    /// return false.
    /// </summary>
    /// <param name="propertyTypeInfo"></param>
    /// <returns></returns>
    public static bool IsGenericEnumerable(TypeInfo propertyTypeInfo)
    {
      return propertyTypeInfo.IsGenericType && propertyTypeInfo.GetInterface("IEnumerable") != null;
    }

    public static Expression InitializeTypeExpression(string[] properties, Expression source, bool flattenSingleProp = false)
    {
      var sourceTypeInfo = source.Type.GetTypeInfo();
      var assignExpressions = new List<MemberAssignment>();
      var propGroups = properties.Select(p => p.Trim().Split(new [] { '.' }, 2)).GroupBy(p => new { propName = p[0], hasSubProps = p.Length > 1 })
        .Select(g =>
        {
          var propInfo = GetProperty(sourceTypeInfo, g.Key.propName);
          if (propInfo == null)
          {
            throw new InvalidOperationException($"'{sourceTypeInfo.FullName}' does not have a property definition for '{g.Key.propName}'");
          }
          return new
          {
            propName = propInfo.Name,
              propInfo = propInfo,
              subProps = g.Key.hasSubProps ? g.Select(p => p[1]).ToArray() : null
          };
        });
      // Check for a property both with and without sub-properties, this isn't valid
      var dup = propGroups.GroupBy(p => p.propName).Select(g => new { propName = g.Key, count = g.Count() }).FirstOrDefault(g => g.count > 1);
      if (dup != null)
      {
        throw new InvalidOperationException($"'{sourceTypeInfo.FullName}' cannot select full property '{dup.propName}' and also some of its sub-properties");
      }
      var dynamicType = DynamicType.GetDynamicType(propGroups.Select(p =>(p.propName, p.propInfo.PropertyType)).ToArray(), false);
      var dynamicTypeInfo = dynamicType.GetTypeInfo();
      foreach (var propGroup in propGroups)
      {
        var propertyTypeInfo = propGroup.propInfo.PropertyType.GetTypeInfo();
        Expression getValue;
        if (propGroup.subProps == null)
        {
          // i.SomeProperty
          getValue = GetPropertyExpression(source, propGroup.propInfo);
        }
        else if (IsGenericEnumerable(propertyTypeInfo))
        {
          // i.SomeListProperty
          var sourceList = Expression.Property(source, propGroup.propInfo);
          var genericType = propertyTypeInfo.GetGenericArguments().First();
          // j
          var innerSelectParam = Expression.Parameter(genericType, "j");
          // new { Prop1 = j.Prop1 }
          var innerSelectObject = InitializeTypeExpression(propGroup.subProps, innerSelectParam);
          // j => new { Prop1 = j.Prop1 }
          var subSelectLambda = Expression.Lambda(innerSelectObject, innerSelectParam);
          // i.SomeListProperty.Select(j => new { Prop1 = j.Prop1 })
          getValue = Expression.Call(
            typeof(Enumerable),
            "Select",
            new [] { genericType, typeof(object) },
            sourceList,
            subSelectLambda
          );

          // (object)i.SomeListProperty.Select(j => new { Prop1 = j.Prop1 })
          getValue = Expression.Convert(getValue, typeof(object));
          // i.SomeListProperty == null ? null : (object)i.SomeListProperty.Select(j => new { Prop1 = j.Prop1 })
          getValue = GetNullConditional(sourceList, getValue);
        }
        else
        {
          // i.SomeProperty
          var property = Expression.Property(source, propGroup.propName);
          // new SomeType { SomeNestedProperty = i.SomeProperty.SomeNestedProperty ... }
          getValue = InitializeTypeExpression(propGroup.subProps, property);

          if (source is MemberExpression)
          {
            // i.SomeProperty == null ? null : new SomeType { SomeNestedProperty = i.SomeProperty.SomeNestedProperty ... }
            getValue = GetNullConditional(source, getValue);
          }
        }

        var dynamicProp = GetProperty(dynamicTypeInfo, propGroup.propName);
        // SomeProperty1 = (object)i.SomeProperty1
        assignExpressions.Add(Expression.Bind(dynamicProp, getValue));
      }

      if (assignExpressions.Count == 1 && flattenSingleProp)
      {
        // i.SomeProperty1
        return assignExpressions[0].Expression;
      }
      else
      {
        // new SomeType() { SomeProperty1 = i.SomeProperty1, ... }
        var instance = Expression.New(dynamicType);
        return Expression.MemberInit(instance, assignExpressions.ToArray());
      }
    }

    /// <summary>
    /// Used to change the parameter used by an expression to a different parameter.
    /// This is primarily used to change the body of a LambdaExpression to use a
    /// parameter that is different than the one originally specified.
    /// </summary>
    private class ParamChanger : ExpressionVisitor
    {
      private Expression _newExpr;
      private Expression _oldParam;

      public ParamChanger(Expression oldParam, Expression newExpr)
      {
        _oldParam = oldParam;
        _newExpr = newExpr;
      }

      protected override Expression VisitParameter(ParameterExpression node)
      {
        if (node == _oldParam)
        {
          return _newExpr;
        }

        return node;
      }
    }
  }
}