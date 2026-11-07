import os, sys, time
ROOT = r"D:\Projects\ACE-Step-1.5"
os.chdir(ROOT); sys.path.insert(0, ROOT)
from acestep.core.generation.handler.init_service_orchestrator import InitServiceOrchestratorMixin as M
_orig = M.initialize_service
def patched(self, *a, **k):
    if k.get("quantization") is None:
        k["quantization"] = "int8_weight_only"
    print("[launcher] initialize_service quantization =", k["quantization"], flush=True)
    return _orig(self, *a, **k)
M.initialize_service = patched
sys.argv = ["acestep-api", "--host", "127.0.0.1", "--port", "8001"]
from acestep.api_server import main
main()
