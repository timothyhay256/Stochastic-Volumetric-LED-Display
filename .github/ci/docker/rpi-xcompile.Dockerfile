# Crosscompilation to Raspberry Pi on RasiOS using system OpenCV
#
# Building this image requries `qemu-arm` to be present on the host system and the corresponding `binfmt-misc` set up (see
# e.g. https://wiki.debian.org/QemuUserEmulation, only `Installing packages` should be enough).
#
# After the successful build you will have an image configured for cross-compilation to Raspberry Pi. It will contain the
# sample build script `/usr/local/bin/cargo-xbuild` that you can check for the correct environment setup and the specific
# command line arguments to use when crosscompiling the project inside the container created from that image.


# Download and extract rpi root filesystem
FROM alpine:3.18

RUN set -xeu && \
    apk add xz

ADD https://downloads.raspberrypi.org/raspios_lite_armhf/root.tar.xz /

RUN set -xeu && \
    mkdir "/rpi-root" && \
    tar xpaf /root.tar.xz -C /rpi-root


# Prepare the root of the rpi filesystem, it's going to be used later for crosscompilation
# This step requries qemu-arm to be present on the host system and the corresponding binfmt-misc set up
FROM scratch

COPY --from=0 /rpi-root /

RUN set -xeu && \
    apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get dist-upgrade -y && \
    apt-get autoremove -y --purge && \
    apt-get -y autoclean

RUN set -xeu && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y symlinks

# error: undefined symbol: _dl_pagesize (and __pointer_chk_guard_local)
# solution: fix the rpi-root symlink /usr/lib/arm-linux-gnueabihf/libpthread.so to be relative and point to ../../../lib/arm-linux-gnueabihf/libpthread.so.0
# source: https://github.com/Azure/azure-iot-sdk-c/issues/1093
RUN set -xeu && \
    symlinks -cr /

# Specify dependencies that you need to have on rpi
RUN set -xeu && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y libudev-dev libsqlite3-dev libstrophe-dev libcamera-dev pkg-config

# Build OpenCV from source for most recent version (There are breaking changes betweeen the required version and the version Debian packages)

# # Install dependencies for building OpenCV

# RUN apt-get update && DEBIAN_FRONTEND=noninteractive apt-get install -y \
#     build-essential cmake \
#     qt5-default libvtk6-dev \
#     zlib1g-dev libjpeg-dev libwebp-dev libpng-dev libtiff5-dev libjasper-dev libopenexr-dev libgdal-dev \
#     libdc1394-22-dev libavcodec-dev libavformat-dev libswscale-dev libtheora-dev libvorbis-dev \
#     libxvidcore-dev libx264-dev yasm libopencore-amrnb-dev libopencore-amrwb-dev libv4l-dev libxine2-dev \
#     libtbb-dev libeigen3-dev \
#     python-dev python-tk python-numpy python3-dev python3-tk python3-numpy \
#     ant default-jdk \
#     doxygen \
#     unzip wget && \
#     apt-get clean && rm -rf /var/lib/apt/lists/*

RUN sudo apt-get install -y \
	build-essential ccache cmake unzip pkg-config curl \
	libjpeg-dev libpng-dev libtiff-dev \
	libavcodec-dev libavformat-dev libswscale-dev libv4l-dev \
	libxvidcore-dev libx264-dev libjasper1 libjasper-dev \
	libgtk-3-dev libcanberra-gtk* \
	libatlas-base-dev gfortran \
	libeigen3-dev libtbb-dev \
    libglfw3-dev libgl1-mesa-dev libglu1-mesa-dev \
	python3-dev python3-numpy python-dev 


# Build and install OpenCV from source
RUN set -eux; \
    wget https://github.com/opencv/opencv/archive/refs/tags/4.12.0.zip && \
    wget -O opencv_contrib.zip https://github.com/opencv/opencv_contrib/archive/refs/tags/4.12.0.zip && \
    unzip 4.12.0.zip && \
    unzip opencv_contrib.zip && \
    rm 4.12.0.zip && \
    rm opencv_contrib.zip && \
    mv opencv-4.12.0 OpenCV && \
    mv opencv_contrib-4.12.0 OpenCV_contrib && \
    mkdir -p OpenCV/build && \
    cd OpenCV/build && \
    # cmake -DWITH_QT=ON \
    #       -DWITH_OPENGL=ON \
    #       -DFORCE_VTK=ON \
    #       -DWITH_TBB=ON \
    #       -DWITH_GDAL=ON \
    #       -DWITH_XINE=ON \
    #       -DBUILD_EXAMPLES=ON .. && \
    cmake .. \
            -DCPACK_BINARY_DEB=ON \
            -DCMAKE_BUILD_TYPE=Release \
            -DCMAKE_INSTALL_PREFIX=/usr/local \
            -DOPENCV_GENERATE_PKGCONFIG=ON \
            -DOPENCV_EXTRA_MODULES_PATH=../../OpenCV_contrib/modules \
            -DOPENCV_VCSVERSION=4.12.0 \
            -DEXTRA_MODULES_VCSVERSION=4.12.0 \
            -DBUILD_opencv_python3=ON \
            -DBUILD_PERF_TESTS=OFF \
            -DBUILD_EXAMPLES=OFF \
            -DBUILD_TESTS=OFF \
            -DBUILD_PACKAGE=ON \
            -DINSTALL_CREATE_DISTRIB=ON \
            -DENABLE_NEON=ON \
            -DENABLE_VFPV3=ON \
            -DOPENCV_ENABLE_NONFREE=ON \
            -DWITH_TBB=ON \
            -DWITH_EIGEN=ON && \
    make -j"$(nproc)" && \
    make install && \
    # cpack -G DEB && \
    # make uninstall && \
    # dpkg -i OpenCV-*.deb && \
    ldconfig

# RUN echo "/usr/local/lib" > /etc/ld.so.conf.d/opencv.conf && ldconfig

# Create the image that will be used for crosscompilation
FROM ubuntu:22.04

COPY --from=1 / /rpi-root

RUN set -xeu && \
    apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get dist-upgrade -y && \
    apt-get autoremove -y --purge && \
    apt-get -y autoclean

RUN set -xeu && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y clang libclang-dev lld curl git build-essential pkg-config cmake

RUN set -xeu && \
    curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile=minimal && \
    rm -rf /root/.rustup/tmp/* # warning: could not delete temp directory: /root/.rustup/tmp/szyc3h06vricp83o_dir

RUN set -xeu && \
    echo "[net]\ngit-fetch-with-cli = true\n[target.arm-unknown-linux-gnueabihf]\nlinker = \"clang-rpi\"" > /root/.cargo/config

ENV PATH="${PATH}:/root/.cargo/bin"

RUN set -xeu && \
    rustup target add arm-unknown-linux-gnueabihf

RUN echo '#!/bin/bash\n\
RPI_ROOT="/rpi-root"\n\
clang --target=arm-unknown-linux-gnueabihf -fuse-ld=lld --sysroot="$RPI_ROOT" --gcc-toolchain="$RPI_ROOT" "$@"' > /usr/local/bin/clang-rpi && chmod +x /usr/local/bin/clang-rpi

# RUN ln -s /rpi-root/usr/local/include/opencv4 /usr/local/include/opencv4 && \
#     ln -s /rpi-root/usr/include /usr/include && \
#     ln -s /rpi-root/usr/local/lib/arm-linux-gnueabihf /usr/local/lib/arm-linux-gnueabihf

RUN ls -R /rpi-root/usr/local/include/opencv4 || echo "opencv4 headers not found"

RUN echo '#!/bin/bash\n\
RPI_ROOT="/rpi-root"\n\
export PKG_CONFIG_SYSROOT_DIR="$RPI_ROOT"\n\
export PKG_CONFIG_LIBDIR="$RPI_ROOT/usr/lib/arm-linux-gnueabihf/pkgconfig"\n\
export OPENCV_HEADER_DIR="$RPI_ROOT/usr/local/include/opencv4"\n\
export OPENCV_INCLUDE_PATHS="$RPI_ROOT/usr/local/include/opencv4"\n\
export OPENCV_LINK_PATHS="$RPI_ROOT/usr/local/lib/arm-linux-gnueabihf"\n\
export OPENCV_LINK_LIBS="opencv_world"\n\
export CC="clang-rpi"\n\
export CXX="clang-rpi"\n\
cargo build -vv --target arm-unknown-linux-gnueabihf' > /usr/local/bin/cargo-xbuild && chmod +x /usr/local/bin/cargo-xbuild
