# SuperDotNet
SuperDotNet is a .Net Library for distributing arbitrary tasks over multiple computers.

How it works:

Project that distributes work

1. Reference the SuperDotNet.dll in your project.
<br/>
2. Create new Simulation: 
<br/><b>string[] node_ids = null;<br/>
int node_count = 4;<br/>
Ic ic = new IC();<br/>
ic.Create_New_Simulation(ref node_ids, node_count,"Simulation_Name",typeof(TestInstance));
</b><br/>
<br/>
node_count is the amount of parallel tasks you want to run,
they get distributed over all connected worker machines.
ic.Create_New_Simulation(...) references node_ids, in which
it returns the string ids of the workers. typeof(TestInstance)
can be any class from your project, an instance of that
class is created on every worker and you can call Methods 
with parameters and get returns of any type.

3. Call a Method in the referenced class (TestInstance)
and run it in parallel over all the worker machines:
<br/>
<b>
string param = "Hello World";<br/>
object[][] data = new object[node_count][]{new object[]{param},...};<br/>
int ret = ic.RunPX<int>(node_ids, "Method_Name", data, null);<br/>
</b><br/>
data is contains the parameters for the functions on all the
different workers. ic.RunPX<INT> int is the return type
of the method called. When you want to return a file 
then the method you call needs to return a string containing
the path to the file that gets returned, and you
replace the null parameter with a string array containing
the file paths to the locations you want to save the files in.
If you want to send files as parameters to the workers
call ic.RunFX<int>(...); and replace object[][] data with 
a string array containing all the file paths, otherwise it 
is the same as RunPX.
<br/>

4. Save Simulation:
<br/>
<b>
ic.Save("./save.sim");
</b><br/>

5. Load Simulation:
<br/>
<b>
ic.Try_Load_Simulation("./save.sim");
</b><br/>


Create Client worker Project

1. Create a new project
2. Reference the SuperDotNet.dll in the new project.
3. Write this Line:	"<b>Comp comp = new Comp("ip.of.host.machine");</b>"
4. Build and run.