# Linux builds of the two native plugins that ship as Windows-only DLLs in
# Assets/5.External_Libs (Ruckig.dll, InverseKinematics.dll). Same C API, so the
# C# wrappers are unchanged; Unity loads lib<Name>.so on Linux.
#   cmake -S NativeLinux -B NativeLinux/build -DCMAKE_BUILD_TYPE=Release
#   cmake --build NativeLinux/build
cmake_minimum_required(VERSION 3.16)
project(tiago_twin_native_linux CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

include(FetchContent)
set(BUILD_SHARED_LIBS OFF CACHE BOOL "" FORCE)
set(BUILD_EXAMPLES OFF CACHE BOOL "" FORCE)
set(BUILD_TESTS OFF CACHE BOOL "" FORCE)
set(BUILD_PYTHON_MODULE OFF CACHE BOOL "" FORCE)
set(BUILD_CLOUD_CLIENT OFF CACHE BOOL "" FORCE)
FetchContent_Declare(ruckig GIT_REPOSITORY https://github.com/pantor/ruckig.git GIT_TAG v0.14.0)
FetchContent_MakeAvailable(ruckig)

set(PLUGINS ${CMAKE_CURRENT_SOURCE_DIR}/../Assets/5.External_Libs)
# Unity's editor may carry another libstdc++: keep the plugins self-contained.
set(STATIC_RUNTIME -static-libstdc++ -static-libgcc)

add_library(Ruckig SHARED src/ruckig_c.cpp)
target_link_libraries(Ruckig PRIVATE ruckig::ruckig)
target_link_options(Ruckig PRIVATE ${STATIC_RUNTIME})
set_target_properties(Ruckig PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PLUGINS}/Ruckig)

add_library(InverseKinematics SHARED src/inverse_kinematics_c.cpp)
target_link_options(InverseKinematics PRIVATE ${STATIC_RUNTIME})
set_target_properties(InverseKinematics PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PLUGINS}/InverseKinematics)
