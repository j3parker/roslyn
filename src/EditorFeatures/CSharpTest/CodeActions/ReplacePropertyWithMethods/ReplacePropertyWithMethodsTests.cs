using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.CodeAnalysis.CSharp.CodeStyle;
using Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeRefactorings;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.ReplacePropertyWithMethods;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.Editor.CSharp.UnitTests.CodeActions.ReplacePropertyWithMethods
{
    public class ReplacePropertyWithMethodsTests : AbstractCSharpCodeActionTest
    {
        protected override CodeRefactoringProvider CreateCodeRefactoringProvider(Workspace workspace, TestParameters parameters)
            => new ReplacePropertyWithMethodsCodeRefactoringProvider();

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestGetWithBody()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPublicProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAnonyousType1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }

    public void M()
    {
        var v = new { P = this.Prop } }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }

    public void M()
    {
        var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAnonyousType2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }

    public void M()
    {
        var v = new { this.Prop } }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }

    public void M()
    {
        var v = new { Prop = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPassedToRef1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }

    public void RefM(ref int i)
    {
    }

    public void M()
    {
        RefM(ref this.Prop);
    }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }

    public void RefM(ref int i)
    {
    }

    public void M()
    {
        RefM(ref this.{|Conflict:GetProp|}());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestPassedToOut1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }

    public void OutM(out int i)
    {
    }

    public void M()
    {
        OutM(out this.Prop);
    }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }

    public void OutM(out int i)
    {
    }

    public void M()
    {
        OutM(out this.{|Conflict:GetProp|}());
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUsedInAttribute1()
        {
            await TestInRegularAndScriptAsync(
@"using System;

class CAttribute : Attribute
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }
}

[C(Prop = 1)]
class D
{
}",
@"using System;

class CAttribute : Attribute
{
    public int GetProp()
    {
        return 0;
    }
}

[C({|Conflict:Prop|} = 1)]
class D
{
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestSetWithBody1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        set
        {
            var v = value;
        }
    }
}",
@"class C
{
    private void SetProp(int value)
    {
        var v = value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestSetReference1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        set
        {
            var v = value;
        }
    }

    void M()
    {
        this.Prop = 1;
    }
}",
@"class C
{
    private void SetProp(int value)
    {
        var v = value;
    }

    void M()
    {
        this.SetProp(1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestGetterAndSetter()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            var v = value;
        }
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestGetterAndSetterAccessibilityChange()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }

        private set
        {
            var v = value;
        }
    }
}",
@"class C
{
    public int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestIncrement1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            var v = value;
        }
    }

    void M()
    {
        this.Prop++;
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }

    void M()
    {
        this.SetProp(this.GetProp() + 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestDecrement2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            var v = value;
        }
    }

    void M()
    {
        this.Prop--;
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }

    void M()
    {
        this.SetProp(this.GetProp() - 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestRecursiveGet()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return this.Prop + 1;
        }
    }
}",
@"class C
{
    private int GetProp()
    {
        return this.GetProp() + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestRecursiveSet()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        set
        {
            this.Prop = value + 1;
        }
    }
}",
@"class C
{
    private void SetProp(int value)
    {
        this.SetProp(value + 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCompoundAssign1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            var v = value;
        }
    }

    void M()
    {
        this.Prop *= x;
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }

    void M()
    {
        this.SetProp(this.GetProp() * x);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCompoundAssign2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            var v = value;
        }
    }

    void M()
    {
        this.Prop *= x + y;
    }
}",
@"class C
{
    private int GetProp()
    {
        return 0;
    }

    private void SetProp(int value)
    {
        var v = value;
    }

    void M()
    {
        this.SetProp(this.GetProp() * (x + y));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestMissingAccessors()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { }

    void M()
    {
        var v = this.Prop;
    }
}",
@"class C
{
    void M()
    {
        var v = this.GetProp();
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestComputedProp()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop => 1;
}",
@"class C
{
    private int GetProp()
    {
        return 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestComputedPropWithTrailingTrivia()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop => 1; // Comment
}",
@"class C
{
    private int GetProp()
    {
        return 1; // Comment
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplaceMethodWithProperty)]
        public async Task TestIndentation()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Foo
    {
        get
        {
            int count;
            foreach (var x in y)
            {
                count += bar;
            }
            return count;
        }
    }
}",
@"class C
{
    private int GetFoo()
    {
        int count;
        foreach (var x in y)
        {
            count += bar;
        }
        return count;
    }
}",
ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestComputedPropWithTrailingTriviaAfterArrow()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop => /* return 42 */ 42;
}",
@"class C
{
    public int GetProp()
    {
        /* return 42 */
        return 42;
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAbstractProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public abstract int [||]Prop { get; }

    public void M()
    {
        var v = new { P = this.Prop } }
}",
@"class C
{
    public abstract int GetProp();

    public void M()
    {
        var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestVirtualProperty()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public virtual int [||]Prop
    {
        get
        {
            return 1;
        }
    }

    public void M()
    {
        var v = new { P = this.Prop } }
}",
@"class C
{
    public virtual int GetProp()
    {
        return 1;
    }

    public void M()
    {
        var v = new { P = this.GetProp() } }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestInterfaceProperty()
        {
            await TestInRegularAndScriptAsync(
@"interface I
{
    int [||]Prop { get; }
}",
@"interface I
{
    int GetProp();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop { get; }
}",
@"class C
{
    private readonly int prop;

    public int GetProp()
    {
        return prop;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop { get; }

    public C()
    {
        this.Prop++;
    }
}",
@"class C
{
    private readonly int prop;

    public int GetProp()
    {
        return prop;
    }

    public C()
    {
        this.prop = this.GetProp() + 1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop { get; }

    public C()
    {
        this.Prop *= x + y;
    }
}",
@"class C
{
    private readonly int prop;

    public int GetProp()
    {
        return prop;
    }

    public C()
    {
        this.prop = this.GetProp() * (x + y);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop { get; } = 1;
}",
@"class C
{
    private readonly int prop = 1;

    public int GetProp()
    {
        return prop;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    private int prop;

    public int [||]Prop { get; } = 1;
}",
@"class C
{
    private int prop;
    private readonly int prop1 = 1;

    public int GetProp()
    {
        return prop1;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestAutoProperty6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]PascalCase { get; }
}",
@"class C
{
    private readonly int pascalCase;

    public int GetPascalCase()
    {
        return pascalCase;
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        get
        {
            return 0;
        }
    }

    public abstract int GetProp();
}",
@"class C
{
    public int GetProp1()
    {
        return 0;
    }

    public abstract int GetProp();
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public int [||]Prop
    {
        set
        {
        }
    }

    public abstract void SetProp(int i);
}",
@"class C
{
    public void SetProp1(int value)
    {
    }

    public abstract void SetProp(int i);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestUniqueName3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    public object [||]Prop
    {
        set
        {
        }
    }

    public abstract void SetProp(dynamic i);
}",
@"class C
{
    public void SetProp1(object value)
    {
    }

    public abstract void SetProp(dynamic i);
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {

        Prop++;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {

        SetProp(GetProp() + 1);
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        /* Leading */
        Prop++; /* Trailing */
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        /* Leading */
        SetProp(GetProp() + 1); /* Trailing */
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestTrivia3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        /* Leading */
        Prop += 1 /* Trailing */ ;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        /* Leading */
        SetProp(GetProp() + 1 /* Trailing */ );
    }
}", ignoreTrivia: false);
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task ReplaceReadInsideWrite1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        Prop = Prop + 1;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        SetProp(GetProp() + 1);
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task ReplaceReadInsideWrite2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }

    void M()
    {
        Prop *= Prop + 1;
    }
}",
@"class C
{
    private int prop;

    private int GetProp()
    {
        return prop;
    }

    private void SetProp(int value)
    {
        prop = value;
    }

    void M()
    {
        SetProp(GetProp() * (GetProp() + 1));
    }
}");
        }

        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        [WorkItem(16157, "https://github.com/dotnet/roslyn/issues/16157")]
        public async Task TestWithConditionalBinding1()
        {
            await TestInRegularAndScriptAsync(
@"public class Foo
{
    public bool [||]Any { get; } // Replace 'Any' with method

    public static void Bar()
    {
        var foo = new Foo();
        bool f = foo?.Any == true;
    }
}",
@"public class Foo
{
    private readonly bool any;

    public bool GetAny()
    {
        return any;
    }

    public static void Bar()
    {
        var foo = new Foo();
        bool f = foo?.GetAny() == true;
    }
}");
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle1()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }
    }
}",
@"class C
{
    private int GetProp() => 0;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle2()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            return 0;
        }

        set
        {
            throw e;
        }
    }
}",
@"class C
{
    private int GetProp() => 0;
    private void SetProp(int value) => throw e;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle3()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get => 0;

        set => throw e;
    }
}",
@"class C
{
    private int GetProp() => 0;
    private void SetProp(int value) => throw e;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle4()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop => 0;
}",
@"class C
{
    private int GetProp() => 0;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle5()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; }
}",
@"class C
{
    private readonly int prop;

    private int GetProp() => prop;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle6()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop { get; set; }
}",
@"class C
{
    private int prop;

    private int GetProp() => prop;
    private void SetProp(int value) => prop = value;
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        [WorkItem(16980, "https://github.com/dotnet/roslyn/issues/16980")]
        [Fact, Trait(Traits.Feature, Traits.Features.CodeActionsReplacePropertyWithMethods)]
        public async Task TestCodeStyle7()
        {
            await TestInRegularAndScriptAsync(
@"class C
{
    int [||]Prop
    {
        get
        {
            A();
            return B();
        }
    }
}",
@"class C
{
    private int GetProp()
    {
        A();
        return B();
    }
}", ignoreTrivia: false, options: PreferExpressionBodiedMethods);
        }

        private IDictionary<OptionKey, object> PreferExpressionBodiedMethods =>
            OptionsSet(SingleOption(CSharpCodeStyleOptions.PreferExpressionBodiedMethods, CodeStyleOptions.TrueWithSuggestionEnforcement));
    }
}